using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;

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
    public class Loop: IDisposable
    {
        #region Static

        public readonly struct YieldAwaitable {
            private readonly CancellationSignal m_Signal;
            private readonly ContinuationQueue m_Queue;
            public YieldAwaitable(ContinuationQueue queue)
            {
                m_Queue = queue;
                m_Signal = default;
            }
            
            public YieldAwaitable(ContinuationQueue queue, CancellationSignal signal)
            {
                m_Queue = queue;
                m_Signal = signal;
            }

            public Awaiter GetAwaiter()
            {
                return new Awaiter(m_Queue, m_Signal);
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
            
            public UniTask ToUniTask(CancellationSignal signal)
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
                private readonly ContinuationQueue m_Queue;
                private readonly CancellationSignal m_Signal;
                // ReSharper disable once InconsistentNaming
                public bool IsCompleted => false;

                public Awaiter(ContinuationQueue queue, CancellationSignal signal = default)
                {
                    m_Queue = queue;
                    m_Signal = signal;
                }

                public void GetResult()
                {
                    if (m_Queue.IsDisposed || m_Signal is { CanBeCanceled: true, IsCancellationRequested: true })
                    {
                        throw new OperationCanceledException("Yield was cancelled");
                    }
                }

                public void OnCompleted(Action continuation)
                {
                    m_Queue.Enqueue(continuation);
                }

                public void UnsafeOnCompleted(Action continuation)
                {
                    m_Queue.Enqueue(continuation);
                }
            }
        }
        

        // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
        public class TimeSource
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static float DefaultTimeGetter()
            {
                return UnityEngine.Time.time;
            }
            public virtual float time => m_TimeGetter();
            
            private readonly Func<float> m_TimeGetter;
            
            public TimeSource(Func<float> timeGetter = null)
            {
                m_TimeGetter = timeGetter ?? DefaultTimeGetter;
            }
        }
        private static readonly TimeSource DefaultTimeSource = new TimeSource();
        #endregion
        #if DEBUG
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once MemberCanBePrivate.Global
        public System.Func<float, bool>[] m_Actions = new System.Func<float, bool>[15];
        #else
        private System.Func<float, bool>[] m_Actions = new System.Func<float, bool>[15];
        #endif
        
        private readonly List<Func<float, bool>> m_PendingAdditions = new List<Func<float, bool>>();
        
        private readonly object m_LockArray = new object();
        private int m_Tail = 0;
        private float m_LastUpdateTime;
        private float m_DeltaTime;
        private int m_FirstRemovedIndex;
        private readonly TimeSource m_TimeSource;
        private bool m_IsProcessing = false;
        private event Action OnDisposed;
        private ContinuationQueue m_ContinuationQueue = new ContinuationQueue();
        public Loop(TimeSource timeSource = null)
        {
            m_TimeSource = timeSource ?? DefaultTimeSource;
            m_LastUpdateTime = m_TimeSource.time;
        }
        public void Update()
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
            m_DeltaTime = m_TimeSource.time - m_LastUpdateTime;
            m_LastUpdateTime = m_TimeSource.time;
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
                            nextResult = action(m_DeltaTime);
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
        
        public UniTask WaitUntil(System.Func<float, bool> action, CancellationSignal cancellationSignal = default)
        {
            var tsc = SafeUniTaskCompletionSource.Create();

            void HandleDispose()
            {
                tsc.TrySetCanceled();
            }
            OnDisposed += HandleDispose;
            bool WaitUntilAction(float deltaTime)
            {
                if (cancellationSignal.IsCancellationRequested)
                {
                    OnDisposed -= HandleDispose;
                    tsc.TrySetCanceled();
                    return true;
                }
                if (action(deltaTime))
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
        
        public UniTask WaitWhile(System.Func<float, bool> action, CancellationSignal cancellationSignal = default)
        {
            var tsc = SafeUniTaskCompletionSource.Create();
            void HandleDispose()
            {
                tsc.TrySetCanceled();
            }
            OnDisposed += HandleDispose;
            bool WaitWhileAction(float deltaTime)
            {
                if (cancellationSignal.IsCancellationRequested)
                {
                    OnDisposed -= HandleDispose;
                    tsc.TrySetCanceled();
                    return true;
                }
                if (!action(deltaTime))
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
        
        public DisposableAction AddAction(System.Action<float> action)
        {
            var active = true;
            bool Action(float delta)
            {
                if (!active) return true;
                action(delta);
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
        
        public YieldAwaitable Yield() => new (m_ContinuationQueue);
        public YieldAwaitable Yield(CancellationSignal signal) => new (m_ContinuationQueue, signal);
        
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
        
        public void ResetTime() => m_LastUpdateTime = m_TimeSource.time;
        
        public void Reset()
        {
            m_Tail = 0;
            m_LastUpdateTime = m_TimeSource.time;
            m_DeltaTime = 0;
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