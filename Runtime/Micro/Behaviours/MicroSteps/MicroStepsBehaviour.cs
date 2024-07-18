using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public class MicroStepsBehaviour : IDisposable, IAsyncDisposable
    {
        public delegate UniTask<bool> NextStepAction();

        public delegate UniTask AsyncAction(NextStepAction next, CancellationToken cancellationToken);

        public static MicroStepsBehaviour Run(AsyncAction action)
        {
            return new MicroStepsBehaviour(action);
        }

        private AsyncAction m_Action;
        private bool m_AsyncDisposed;
        private CancellationTokenSource m_DisposeCancellationTokenSource;
        private UniTaskCompletionSource m_CompletionSource;
        private UniTaskCompletionSource m_StepCompletionSource;
        private UniTaskCompletionSource<bool> m_NextStepCompletionSource;

        public MicroStepsBehaviour(AsyncAction action)
        {
            m_Action = action;
            m_DisposeCancellationTokenSource = new CancellationTokenSource();
            RunAsync();
        }

        ~MicroStepsBehaviour()
        {
            Dispose(true);
        }

        private async void RunAsync()
        {
            m_StepCompletionSource = new UniTaskCompletionSource();
            m_NextStepCompletionSource = new UniTaskCompletionSource<bool>();
            m_CompletionSource = new UniTaskCompletionSource();
            m_DisposeCancellationTokenSource.Token.Register(() =>
            {
                m_NextStepCompletionSource.TrySetCanceled();
                m_StepCompletionSource.TrySetCanceled();
            });
            try
            {
                await m_Action(Internal_WhenNext, m_DisposeCancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                m_StepCompletionSource.TrySetException(e);
                m_CompletionSource.TrySetException(e);
            }
            finally
            {
                m_CompletionSource.TrySetResult();
            }
        }

        /// <summary>
        /// You should call WhenNext() to wait for the action to proceed to the next step from the outside
        /// </summary>
        /// <returns></returns>
        private UniTask<bool> Internal_WhenNext()
        {
            m_StepCompletionSource.TrySetResult();
            return m_NextStepCompletionSource.Task;
        }

        /// <summary>
        /// You should call Next() to perform next step inside behaviour instance and wait for it to complete
        /// </summary>
        /// <returns></returns>
        public async UniTask Next(CancellationToken cancellationToken = default)
        {
            var registration = cancellationToken.Register(() =>
            {
                m_DisposeCancellationTokenSource.Cancel();
                m_StepCompletionSource.TrySetCanceled(cancellationToken);
            });
            m_StepCompletionSource = new UniTaskCompletionSource();
            m_NextStepCompletionSource.TrySetResult(true);
            m_NextStepCompletionSource = new UniTaskCompletionSource<bool>();
            try
            {
                await m_StepCompletionSource.Task;
            }
            finally
            {
                registration.Dispose();
            }
        }

        public UniTask Complete()
        {
            m_NextStepCompletionSource.TrySetResult(false);
            return m_CompletionSource.Task;
        }

        public void Dispose()
        {
            Dispose(false);
        }

        public void Dispose(bool isCalledFromFinalizer, bool isAsync = false)
        {
            if (isCalledFromFinalizer)
            {
                throw new InvalidOperationException("You should dispose MicroStepsBehaviour manually");
            }

            if (m_DisposeCancellationTokenSource.IsCancellationRequested)
            {
#if DEBUG
                throw new InvalidOperationException("MicroStepsBehaviour is already disposed");
#endif
                return;
            }

            m_DisposeCancellationTokenSource.Cancel();
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            Dispose(false, true);
            await m_CompletionSource.Task;
        }
    }
}