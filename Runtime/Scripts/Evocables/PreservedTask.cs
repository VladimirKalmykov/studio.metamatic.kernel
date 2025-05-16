using System;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace Metamatic
{
    public readonly struct PreservedTask<T>: IDisposable
    {
        private readonly InternalTaskLike<T> m_LazyTask;
        private readonly short m_Version;
        
        public PreservedTask(UniTask<T> uniTask)
        {
            m_LazyTask = ParamLessPool<InternalTaskLike<T>>.GetShared();
            m_LazyTask.Initialize(uniTask);
            m_Version = m_LazyTask.version;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Awaiter GetAwaiter()
        {
            if (!IsValid())
            {
                return new Awaiter();
            }
            return new Awaiter(m_LazyTask);
        }

        public CancellableTaskLike Wait(Signal cancellationSignal)
        {
            if (!IsValid())
            {
                return new CancellableTaskLike(cancellationSignal);
            }
            return new CancellableTaskLike(m_LazyTask, cancellationSignal);
        }
        
        [UsedImplicitly]
        public UniTask<T> ToUniTask(Signal cancellationSignal = default)
        {
            if (cancellationSignal.isRaised) return UniTask.FromCanceled<T>();
            if (IsValid())
            {
                switch (m_LazyTask.state)
                {
                    case InternalTaskLike<T>.State.Completed:
                        return UniTask.FromResult(m_LazyTask.result);
                    case InternalTaskLike<T>.State.Faulted:
                        return UniTask.FromException<T>(m_LazyTask.exception);
                }
                var tsc = SafeUniTaskCompletionSource<T>.Create();
                var lazyTask = m_LazyTask;
                var version = m_Version;
                m_LazyTask.OnCompleted += cancellationSignal.Register(tsc.TrySetCanceledAndForget);
                m_LazyTask.OnCompleted += () =>
                {
                    if (cancellationSignal.isRaised)
                    {
                        tsc.TrySetCanceled();
                    } else if (lazyTask.version != version)
                    {
                        tsc.TrySetException(new Exception("The lazy task has been reset."));
                    } else if (lazyTask.state == InternalTaskLike<T>.State.Faulted)
                    {
                        tsc.TrySetException(lazyTask.exception);
                    }
                    else
                    {
                        tsc.TrySetResult(lazyTask.result);
                    }
                };
                return tsc.Task;
            }
            return UniTask.FromException<T>(new Exception("The lazy task has been reset."));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsValid()
        {
            return m_Version >= 0 && m_LazyTask.version == m_Version;
        }

        public readonly struct CancellableTaskLike
        {
            private readonly InternalTaskLike<T> m_LazyTask;
            private readonly short m_Version;
            private readonly Signal m_CancellationSignal;

            public CancellableTaskLike(Signal cancellationSignal = default)
            {
                m_LazyTask = null;
                m_CancellationSignal = default;
                m_Version = -1;
            }
            
            public CancellableTaskLike(InternalTaskLike<T> lazyTask, Signal cancellationSignal = default)
            {
                m_LazyTask = lazyTask;
                m_CancellationSignal = cancellationSignal;
                m_Version = m_LazyTask.version;
            }
            
            private bool IsValid()
            {
                return m_Version >= 0 && m_LazyTask.version == m_Version;
            }
            
            public Awaiter GetAwaiter()
            {
                if (!IsValid())
                {
                    return new Awaiter(m_CancellationSignal);
                }
                return new Awaiter(m_LazyTask, m_CancellationSignal);
            }
            
            public UniTask<TOutput> ContinueWith<TOutput>(Func<T, UniTask<TOutput>> continuationFunction) 
            {
                if (m_CancellationSignal.isRaised)
                {
                    return UniTask.FromCanceled<TOutput>();
                }
                if (!IsValid())
                {
                    return UniTask.FromException<TOutput>(new Exception("The lazy task has been reset."));
                }
                return m_LazyTask.ContinueWith(continuationFunction);
            }
            
            public UniTask<TOutput> ContinueWith<TOutput>(Func<T,TOutput> continuationFunction) 
            {
                if (m_CancellationSignal.isRaised)
                {
                    return UniTask.FromCanceled<TOutput>();
                }
                if (!IsValid())
                {
                    return UniTask.FromException<TOutput>(new Exception("The lazy task has been reset."));
                }
                return m_LazyTask.ContinueWith(continuationFunction);
            }
        }
        
        public void Dispose()
        {
            if (IsValid())
            {
                ParamLessPool<InternalTaskLike<T>>.ReturnShared(m_LazyTask);
            }
        }
        
        public readonly struct Awaiter : ICriticalNotifyCompletion
        {
            private readonly InternalTaskLike<T> m_LazyTask;
            private readonly Signal m_CancellationSignal;
            private readonly short m_Version;

            public Awaiter(Signal cancellationSignal = default)
            {
                m_LazyTask = null;
                m_Version = -1;
                m_CancellationSignal = cancellationSignal;
            }
            
            public Awaiter(InternalTaskLike<T> task, Signal cancellationSignal = default)
            {
                m_LazyTask = task;
                m_Version = task.version;
                m_CancellationSignal = cancellationSignal;
            }
            
            private bool IsValid()
            {
                return m_Version >= 0 && m_LazyTask.version == m_Version;
            }
            
            public bool IsCompleted
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_CancellationSignal.isRaised || !IsValid() || m_LazyTask.state != InternalTaskLike<T>.State.Pending;
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T GetResult()
            {
                if (m_CancellationSignal.isRaised)
                {
                    throw new OperationCanceledException();
                }
                if (!IsValid())
                {
                    Debug.LogWarning("The lazy task has been reset.");
                    return default;
                }
                if (m_LazyTask.state == InternalTaskLike<T>.State.Faulted)
                {
                    throw m_LazyTask.exception;
                }
                return m_LazyTask.result;
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnCompleted(Action continuation)
            {
                if (m_CancellationSignal.isRaised)
                {
                    continuation();
                    return;
                }
                if (!IsValid())
                {
                    Debug.LogWarning("The lazy task has been reset.");
                    continuation();
                    return;
                }
                if (m_LazyTask.state != InternalTaskLike<T>.State.Pending)
                {
                    continuation();
                    return;
                }
                m_LazyTask.OnCompleted += continuation;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void UnsafeOnCompleted(Action continuation)
            {
                if (m_CancellationSignal.isRaised)
                {
                    continuation();
                    return;
                }
                if (!IsValid())
                {
                    Debug.LogWarning("The lazy task has been reset.");
                    continuation();
                    return;
                }
                if (m_LazyTask.state != InternalTaskLike<T>.State.Pending)
                {
                    continuation();
                    return;
                }
                m_LazyTask.OnCompleted += continuation;
            }
        }
    }
    
    public class InternalTaskLike<T>: IResettable
    {
        public enum State
        {
            Pending,
            Completed,
            Faulted
        }
        public T result { get; private set; }
        public Exception exception { get; private set; }
        public State state { get; private set; }
        public short version { get; private set; }
        public event Action OnCompleted;
        
        public void Initialize(UniTask<T> uniTask)
        {
            HandleTaskAsync(uniTask).Forget();
        }
        
        private async UniTaskVoid HandleTaskAsync(UniTask<T> uniTask)
        {
            var startVersion = version;
            var valid = true;
            try
            {
                result = await uniTask;
                valid = version == startVersion;
                if (valid)
                {
                    state = State.Completed;
                }
            }
            catch (Exception e)
            {
                valid = version == startVersion;
                if (valid)
                {
                    Console.Warning("TaskLike", "The lazy task has been reset.");
                    return;
                }
                exception = e;
                state = State.Faulted;
            }
            finally
            {
                if (valid)
                {
                    OnCompleted?.Invoke();
                    OnCompleted = null;
                }
            }
        }

        public UniTask<TOutput> ContinueWith<TOutput>(Func<T, UniTask<TOutput>> continuationFunction)
        {
            if (state == State.Completed)
            {
                return continuationFunction(result);
            }
            if (state == State.Faulted)
            {
                return UniTask.FromException<TOutput>(exception);
            }
            var tsc = SafeUniTaskCompletionSource<TOutput>.Create();
            OnCompleted += () =>
            {
                if (state == State.Completed)
                {
                    tsc.TrySetResult(continuationFunction(result).GetAwaiter().GetResult());
                }
                else
                {
                    tsc.TrySetException(exception);
                }
            };
            return tsc.Task;
        }
        
        public UniTask<TOutput> ContinueWith<TOutput>(Func<T, TOutput> continuationFunction)
        {
            if (state == State.Completed)
            {
                return UniTask.FromResult(continuationFunction(result));
            }
            if (state == State.Faulted)
            {
                return UniTask.FromException<TOutput>(exception);
            }
            var tsc = SafeUniTaskCompletionSource<TOutput>.Create();
            OnCompleted += () =>
            {
                if (state == State.Completed)
                {
                    tsc.TrySetResult(continuationFunction(result));
                }
                else
                {
                    tsc.TrySetException(exception);
                }
            };
            return tsc.Task;
        }

        public void Reset()
        {
            unchecked
            {
                version++;
            }

            if (state == State.Pending)
            {
                exception = new Exception("The lazy task has been reset. But there are still pending continuations.");
                state = State.Faulted;
                OnCompleted?.Invoke();
            }
            OnCompleted = null;
            result = default;
            exception = null;
            state = State.Pending;
        }
    }
}