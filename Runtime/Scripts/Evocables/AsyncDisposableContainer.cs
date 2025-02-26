using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public abstract class AsyncDisposableContainer<TResult>: IDisposable
    {
        protected SafeUniTaskCompletionSource m_CompletionSource;
        protected TResult Result;
        protected SignalSource m_DisposeSignalSource = default;
        protected bool Resolved;
        private IDisposable m_DisposableImplementation;
        public void Dispose()
        {
            m_DisposeSignalSource.TryRaise();
        }
    }
    public class AsyncDisposableContainer<TInput, TResult>: AsyncDisposableContainer<TResult>
    {
        public async UniTaskVoid Initialize(Func<Action<TResult>, TInput, Signal, UniTask> initializer, TInput input)
        {
            try
            {
                m_DisposeSignalSource = new SignalSource();
                m_CompletionSource = SafeUniTaskCompletionSource.Create();
                await initializer(SetResult, input, m_DisposeSignalSource.Signal);
            }
            catch (OperationCanceledException)
            {
                m_CompletionSource.TrySetCanceled();
            }
            catch (Exception e)
            {
                m_CompletionSource.TrySetException(e);
            }
            finally
            {
                m_CompletionSource.Task.Forget(); // Release the task source
            }
        }

        public async UniTask<TResult> GetContent(Signal cancellationSignal)
        {
            if (Resolved)
            {
                return Result;
            }
            var winnerIndex = await UniTask.WhenAny(m_CompletionSource.Task, cancellationSignal.ToUniTask());
            if (winnerIndex == 0)
            {
                return Result;
            }
            throw new OperationCanceledException();
        }
        
        private void SetResult(TResult result)
        {
            Resolved = true;
            m_CompletionSource.TrySetResult();
            m_CompletionSource.Task.Forget(); // Release the task source
            Result = result;
        }
    }
}