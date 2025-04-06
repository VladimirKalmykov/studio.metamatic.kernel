using System;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

namespace Metamatic
{
    /// <summary>
    /// Allows to execute async method and get its result via callback function without waiting for its completion.
    /// Async method can continue its work after the callback is called.
    /// </summary>
    public readonly struct AsyncWork
    {
        private readonly SafeUniTaskCompletionSource m_CompletionSource;
        [UsedImplicitly]
        // ReSharper disable once InconsistentNaming
        public UniTask Task => m_CompletionSource.Task;
        
        public AsyncWork(Func<Action, Signal, UniTask> action, Signal cancellationSignal)
        {
            m_CompletionSource = SafeUniTaskCompletionSource.Create();
            InternalProcess(action, cancellationSignal).Forget();
        }

        private async UniTaskVoid InternalProcess(Func<Action, Signal, UniTask> action, Signal cancellationSignal)
        {
            try
            {
                await action(m_CompletionSource.TrySetResultSilently, cancellationSignal);
                m_CompletionSource.TrySetResultSilently();
            }
            catch (OperationCanceledException)
            {
                m_CompletionSource.TrySetCanceled();
            } catch (Exception e)
            {
                m_CompletionSource.TrySetException(e);
            }
        }
        
        // GetAwaiter is required for await keyword to work
        public UniTask.Awaiter GetAwaiter()
        {
            return Task.GetAwaiter();
        }
    }
    
    public readonly struct AsyncWorkWithParam<TParam>
    {
        private readonly SafeUniTaskCompletionSource m_CompletionSource;
        [UsedImplicitly]
        // ReSharper disable once InconsistentNaming
        public UniTask Task => m_CompletionSource.Task;
        
        public AsyncWorkWithParam(Func<Action, TParam, Signal, UniTask> action, TParam param, Signal cancellationSignal)
        {
            m_CompletionSource = SafeUniTaskCompletionSource.Create();
            InternalProcess(action, param, cancellationSignal).Forget();
        }

        private async UniTaskVoid InternalProcess(Func<Action, TParam, Signal, UniTask> action, TParam param, Signal cancellationSignal)
        {
            try
            {
                await action(m_CompletionSource.TrySetResultSilently, param, cancellationSignal);
                m_CompletionSource.TrySetResultSilently();
            }
            catch (OperationCanceledException)
            {
                m_CompletionSource.TrySetCanceled();
            } catch (Exception e)
            {
                m_CompletionSource.TrySetException(e);
            }
        }
        
        // GetAwaiter is required for await keyword to work
        public UniTask.Awaiter GetAwaiter()
        {
            return Task.GetAwaiter();
        }
    }
    
    public readonly struct AsyncWork<T> where T: struct
    {
        private readonly SafeUniTaskCompletionSource<T> m_CompletionSource;
        [UsedImplicitly]
        // ReSharper disable once InconsistentNaming
        public UniTask<T> Task => m_CompletionSource.Task;
        
        public AsyncWork(Func<Action<T>, Signal, UniTask> action, Signal cancellationSignal)
        {
            m_CompletionSource = SafeUniTaskCompletionSource<T>.Create();
            InternalProcess(action, cancellationSignal).Forget();
        }

        private async UniTaskVoid InternalProcess(Func<Action<T>, Signal, UniTask> action, Signal cancellationSignal)
        {
            try
            {
                await action(m_CompletionSource.TrySetResultSilently, cancellationSignal);
                m_CompletionSource.TrySetResultSilently(default);
            }
            catch (OperationCanceledException)
            {
                m_CompletionSource.TrySetCanceled();
            } catch (Exception e)
            {
                m_CompletionSource.TrySetException(e);
            }
        }
        
        // GetAwaiter is required for await keyword to work
        public UniTask<T>.Awaiter GetAwaiter()
        {
            return Task.GetAwaiter();
        }
    }
}