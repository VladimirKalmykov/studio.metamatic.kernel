using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

namespace Metamatic
{
    /// <summary>
    /// Loop class is a simple class that allows you to add actions that will be executed on each update call.
    /// It works like PlayerLoop in Unity, but it's more flexible and can be used in any context.
    /// Also, it allows you to specify custom time source that can be useful for testing and cases when you need to control time.
    /// E.g. you can use it to reverse time, fast forward, or pause time.
    /// There are three methods to add actions: AddAction and WaitUntil, WaitWhile (like in UniTask).
    /// It doesn't support nested loops, but you can create multiple instances of Loop class.
    /// It doesn't support multi-threading, but you can use it in a single-threaded environment in Unity.
    /// it is targeted for Unity, and high performance is a priority.
    /// </summary>
    public class Loop: IDisposable, ILoop
    {
        #region Static
        public readonly struct YieldAwaitable {
            private readonly Signal m_Signal; // TODO: Do we need this signal?
            private readonly ContinuationQueue m_Queue;
            /// <summary>
            /// Directly convert from Custom awaiter
            /// </summary>
            [CanBeNull] private readonly ICriticalNotifyCompletion m_CustomAwaiter;
            public YieldAwaitable(ContinuationQueue queue)
            {
                m_Queue = queue;
                m_Signal = default;
                m_CustomAwaiter = null;
            }
            
            public YieldAwaitable(ContinuationQueue queue, Signal signal)
            {
                m_Queue = queue;
                m_Signal = signal;
                m_CustomAwaiter = null;
            }
            
            public YieldAwaitable(ICriticalNotifyCompletion customAwaiter)
            {
                m_Queue = null;
                m_Signal = default;
                m_CustomAwaiter = customAwaiter;
            }
            
            public YieldAwaitable(ICriticalNotifyCompletion customAwaiter, Signal signal)
            {
                m_Queue = null;
                m_Signal = signal;
                m_CustomAwaiter = customAwaiter;
            }
            
            public Awaiter GetAwaiter()
            {
                return m_CustomAwaiter != null ? new Awaiter(GetCustomAwaiterResult, OnCustomAwaiterCompleted) : new Awaiter(GetContinuationQueueResult, OnContinuationQueueCompleted);
            }
            
            private void GetCustomAwaiterResult()
            {
                if (m_Signal is { CanBeCanceled: true, IsCancellationRequested: true })
                {
                    throw new OperationCanceledException("Yield was cancelled");
                }
            }
            
            private void OnCustomAwaiterCompleted(Action continuation)
            {
                // ReSharper disable once PossibleNullReferenceException
                m_CustomAwaiter.OnCompleted(continuation);
            }

            public Awaiter GetContinuationQueueAwaiter()
            {
                return new Awaiter(GetContinuationQueueResult, OnContinuationQueueCompleted);
            }
            
            public Awaiter GetUniTaskAwaiter()
            {
                return new Awaiter(GetContinuationQueueResult, OnContinuationQueueCompleted);
            }
            
            public void GetContinuationQueueResult()
            {
                if (m_Queue.IsDisposed || m_Signal is { CanBeCanceled: true, IsCancellationRequested: true })
                {
                    throw new OperationCanceledException("Yield was cancelled");
                }
            }
            
            public void OnContinuationQueueCompleted(Action continuation)
            {
                m_Queue.Enqueue(continuation);
            }
            
            public UniTask ToUniTask()
            {
                var queue = m_Queue;
                if (queue.IsDisposed)
                {
                    return UniTask.FromCanceled();
                }
                var tsc = SafeUniTaskCompletionSource.Create();
                void HandleComplete()
                {
                    if (queue.IsDisposed)
                    {
                        tsc.TrySetCanceled();
                        return;
                    }
                    tsc.TrySetResult();
                }
                m_Queue.Enqueue(HandleComplete);
                return tsc.Task;
            }
            
            public UniTask ToUniTask(Signal signal)
            {
                var queue = m_Queue;
                if (m_Queue.IsDisposed)
                {
                    return UniTask.FromCanceled();
                }
                var tsc = SafeUniTaskCompletionSource.Create();
                DisposableAction signalListener = signal.Register(() =>
                {
                    tsc.TrySetCanceled();
                });
                void HandleComplete()
                {
                    if (queue.IsDisposed)
                    {
                        signalListener.Dispose();
                        tsc.TrySetCanceled();
                        return;
                    }
                    signalListener.Dispose();
                    tsc.TrySetResult();
                }
                m_Queue.Enqueue(HandleComplete);
                return tsc.Task;
            }
            
            public readonly struct Awaiter : ICriticalNotifyCompletion
            {
                private readonly Action m_GetResult;
                private readonly Action<Action> m_OnCompleted;
                // ReSharper disable once InconsistentNaming
                public bool IsCompleted => false;

                public Awaiter(Action getResult, Action<Action> onCompleted)
                {
                    m_GetResult = getResult;
                    m_OnCompleted = onCompleted;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public void GetResult()
                {
                    m_GetResult();
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public void OnCompleted(Action continuation)
                {
                    m_OnCompleted(continuation);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public void UnsafeOnCompleted(Action continuation)
                {
                    m_OnCompleted(continuation);
                }
            }
        }
        

        
        private static readonly TimeSource DefaultTimeSource = new TimeSource();
        #endregion
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once MemberCanBePrivate.Global
        public System.Func<bool>[] m_Actions = new System.Func<bool>[15];
        
        private readonly List<Func<bool>> m_PendingAdditions = new List<Func<bool>>();
        
        private readonly object m_LockArray = new object();
        private int m_Tail = 0;
        private int m_FirstRemovedIndex;
        private readonly TimeSource m_TimeSource;
        private bool m_IsProcessing = false;
        private event Action OnDisposed;
        private ContinuationQueue m_ContinuationQueue = new ContinuationQueue();
        
        public Loop(TimeSource timeSource = null)
        {
            m_TimeSource = timeSource ?? DefaultTimeSource;
        }
        
        public void Run()
        {
            m_ContinuationQueue.Run();
            m_IsProcessing = true;
            if (m_PendingAdditions.Count > 0)
            {
                lock (m_LockArray)
                {
                    // Обрабатываем отложенные добавления
                    foreach (var action in m_PendingAdditions)
                    {
                        if (m_Tail == m_Actions.Length)
                        {
                            Array.Resize(ref m_Actions, m_Actions.Length * 2);
                        }
                        m_Actions[m_Tail++] = action;
                    }
                    m_PendingAdditions.Clear();
                }
            }
            if (m_Tail == 0)
            {
                m_IsProcessing = false;
                return;
            }
            m_FirstRemovedIndex = -1;
            lock (m_LockArray)
            {
                bool nextResult = false;
                for (int i = 0; i < m_Tail; i++)
                {
                    var action = m_Actions[i];
                    if (action != null)
                    {
                        try
                        {
                            nextResult = action();
                        } catch (Exception e)
                        {
                            Console.Exception("Loop", "Update", e);
                            nextResult = false;
                        }
                        if (nextResult)
                        {
                            // Remove action
                            m_Actions[i] = null;
                            if (m_FirstRemovedIndex == -1)
                            {
                                m_FirstRemovedIndex = i;
                            }
                        }
                    }
#if DEBUG
                    else
                    {
                        Console.Exception("Loop", "Update", "Action is null");
                    }
#endif
                }

                CleanUpArray(m_FirstRemovedIndex);
            }
            
            m_IsProcessing = false;
        }
        
        public UniTask WaitUntil(System.Func<bool> predicate, Signal cancellationSignal = default)
        {
            var tsc = SafeUniTaskCompletionSource.Create();

            void HandleDispose()
            {
                tsc.TrySetCanceled();
            }
            OnDisposed += HandleDispose;
            bool WaitUntilAction()
            {
                if (cancellationSignal.IsCancellationRequested)
                {
                    OnDisposed -= HandleDispose;
                    tsc.TrySetCanceled();
                    return true;
                }
                if (predicate())
                {
                    OnDisposed -= HandleDispose;
                    tsc.TrySetResult();
                    return true;
                }

                return false;
            }

            if (m_IsProcessing)
            {
                m_PendingAdditions.Add(WaitUntilAction);
            }
            else
            {
                lock (m_LockArray)
                {
                    if (m_Tail == m_Actions.Length)
                    {
                        System.Array.Resize(ref m_Actions, m_Actions.Length * 2);
                    }
                    m_Actions[m_Tail] = WaitUntilAction;
                    m_Tail++;
                }
            }
            
            return tsc.Task;
        }
        
        public UniTask WaitWhile(System.Func<bool> action, Signal cancellationSignal = default)
        {
            var tsc = SafeUniTaskCompletionSource.Create();
            void HandleDispose()
            {
                tsc.TrySetCanceled();
            }
            OnDisposed += HandleDispose;
            bool WaitWhileAction()
            {
                if (cancellationSignal.IsCancellationRequested)
                {
                    OnDisposed -= HandleDispose;
                    tsc.TrySetCanceled();
                    return true;
                }
                if (!action())
                {
                    OnDisposed -= HandleDispose;
                    tsc.TrySetResult();
                    return true;
                }

                return false;
            }

            if (m_IsProcessing)
            {
                m_PendingAdditions.Add(WaitWhileAction);
            }
            else
            {
                lock (m_LockArray)
                {
                    if (m_Tail == m_Actions.Length)
                    {
                        System.Array.Resize(ref m_Actions, m_Actions.Length * 2);
                    }
                    m_Actions[m_Tail] = WaitWhileAction;
                    m_Tail++;
                }
            }
            
            return tsc.Task;
        }
        
        public DisposableAction AddAction(System.Action action)
        {
            var active = true;
            bool Action()
            {
                if (!active) return true; // TODO: Decide about this moment. Should we prevent of executing action right after disposing within the same loop?
                action();
                return false;
            }

            lock (m_LockArray)
            {
                if (m_Tail == m_Actions.Length)
                {
                    Array.Resize(ref m_Actions, m_Actions.Length * 2);
                }
                m_Actions[m_Tail] = Action;
                m_Tail++;
            }
            return new DisposableAction(() =>
            {
                active = false;
            });
        }

        public UniTask Delay(TimeSpan timeSpan, Signal cancellationSignal = default)
        {
            return Delay((float) timeSpan.TotalSeconds, cancellationSignal);
        }
        
        public UniTask Delay(float seconds, Signal cancellationSignal = default)
        {
            var tsc = SafeUniTaskCompletionSource.Create();
            void HandleDispose()
            {
                tsc.TrySetCanceled();
            }
            OnDisposed += HandleDispose;
            var startTime = m_TimeSource.time;
            bool DelayAction()
            {
                if (cancellationSignal.IsCancellationRequested)
                {
                    OnDisposed -= HandleDispose;
                    tsc.TrySetCanceled();
                    return true;
                }
                if (m_TimeSource.time - startTime >= seconds)
                {
                    OnDisposed -= HandleDispose;
                    tsc.TrySetResult();
                    return true;
                }

                return false;
            }

            if (m_IsProcessing)
            {
                m_PendingAdditions.Add(DelayAction);
            }
            else
            {
                lock (m_LockArray)
                {
                    if (m_Tail == m_Actions.Length)
                    {
                        System.Array.Resize(ref m_Actions, m_Actions.Length * 2);
                    }
                    m_Actions[m_Tail] = DelayAction;
                    m_Tail++;
                }
            }
            
            return tsc.Task;
        }
        
        public YieldAwaitable Yield() => new (m_ContinuationQueue);
        public YieldAwaitable Yield(Signal signal) => new (m_ContinuationQueue, signal);
        
        private void CleanUpArray(int firstRemovedIndex)
        {
            if (firstRemovedIndex != -1)
            {
                int newTail = firstRemovedIndex;
                for (int i = firstRemovedIndex; i < m_Tail; i++)
                {
                    if (m_Actions[i] != null)
                    {
                        if (newTail != i)
                        {
                            m_Actions[newTail] = m_Actions[i];
                            m_Actions[i] = null;
                        }
                        newTail++;
                    }
                }
                m_Tail = newTail;
            }
        }
        
        public void Reset()
        {
            m_Tail = 0;
            m_FirstRemovedIndex = -1;
            m_ContinuationQueue.Clear();
        }

        public void Dispose()
        {
            m_ContinuationQueue.Clear();
            OnDisposed?.Invoke();
            OnDisposed = null;
        }
    }
}