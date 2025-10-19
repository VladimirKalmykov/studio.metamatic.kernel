using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    /// <summary>
    /// This struct is awaitable, disposable and provides a progress tracker.
    /// </summary>
    [DevStatus(DevRating.Experimental)]
    public readonly struct TrackableAwaitableDisposable: IDisposable
    {
        // ReSharper disable once InconsistentNaming
        public static TrackableAwaitableDisposable Completed => new TrackableAwaitableDisposable(() => UniTask.CompletedTask.GetAwaiter(), () => { }, ProgressSource.Empty);
        public static TrackableAwaitableDisposable Create(Func<Action, ProgressSource, Signal, UniTask> handler, Signal externalCancellationSignal)
        {
            var progressSource = ProgressSource.Create();
            var signalSource = SignalSource.CreateLinked(externalCancellationSignal);
            var taskSource = SafeUniTaskCompletionSource.Create();
            var handle = handler(() =>
            {
                taskSource.TrySetResult();
            }, progressSource, signalSource.Signal);
            ProcessHandle(handle, signalSource, taskSource).Forget();

            return new TrackableAwaitableDisposable(() => taskSource.Task.GetAwaiter(), signalSource.TryRaise, progressSource);
        }

        private static async UniTaskVoid ProcessHandle(UniTask handle, SignalSource signalSource, SafeUniTaskCompletionSource taskSource)
        {
            try
            {
                await handle;
            }
            catch (OperationCanceledException)
            {
                taskSource.TrySetCanceled();
            }
            catch (Exception e)
            {
                taskSource.TrySetException(e);
            }
            finally
            {
                taskSource.TrySetCanceled();
                signalSource.Dispose();
            }
        }
        
        private readonly Func<UniTask.Awaiter> m_GetAwaiter;
        private readonly Action m_DisposeHandler;
        private readonly ProgressSource m_ProgressSource;

        public Progress progress => m_ProgressSource.Progress;

        public TrackableAwaitableDisposable(Func<UniTask.Awaiter> getAwaiter, Action disposeHandler, ProgressSource progress)
        {
            m_GetAwaiter = getAwaiter;
            m_DisposeHandler = disposeHandler;
            m_ProgressSource = progress;
        }
        
        public UniTask.Awaiter GetAwaiter()
        {
            return m_GetAwaiter();
        }

        public async UniTask ToUniTask()
        {
            await this;
        }

        public void Dispose()
        {
            m_DisposeHandler.Invoke();
            m_ProgressSource.Dispose();
        }
    }
    
    /// <summary>
    /// This struct is awaitable, disposable and provides a progress tracker.
    /// </summary>
    [DevStatus(DevRating.Experimental)]
    public readonly struct TrackableAwaitableDisposable<T>: IDisposable
    {
        public static TrackableAwaitableDisposable<T> Create(Func<Action<T>, ProgressSource, Signal, UniTask> handler, Signal externalCancellationSignal)
        {
            var progressSource = ProgressSource.Create();
            var signalSource = SignalSource.CreateLinked(externalCancellationSignal);
            var taskSource = SafeUniTaskCompletionSource<T>.Create();
            var handle = handler((payload) =>
            {
                taskSource.TrySetResult(payload);
            }, progressSource, signalSource.Signal);
            ProcessHandle(handle, signalSource, taskSource).Forget();

            return new TrackableAwaitableDisposable<T>(() => taskSource.Task.GetAwaiter(), signalSource.TryRaise, progressSource);
        }

        private static async UniTaskVoid ProcessHandle(UniTask handle, SignalSource signalSource, SafeUniTaskCompletionSource<T> taskSource)
        {
            try
            {
                await handle;
            }
            catch (OperationCanceledException)
            {
                taskSource.TrySetCanceled();
            }
            catch (Exception e)
            {
                taskSource.TrySetException(e);
            }
            finally
            {
                taskSource.TrySetCanceled();
                signalSource.Dispose();
            }
        }
        
        private readonly Func<UniTask<T>.Awaiter> m_GetAwaiter;
        private readonly Action m_DisposeHandler;
        private readonly ProgressSource m_ProgressSource;

        public Progress progress => m_ProgressSource.Progress;

        public TrackableAwaitableDisposable(Func<UniTask<T>.Awaiter> getAwaiter, Action disposeHandler, ProgressSource progress)
        {
            m_GetAwaiter = getAwaiter;
            m_DisposeHandler = disposeHandler;
            m_ProgressSource = progress;
        }
        
        public UniTask<T>.Awaiter GetAwaiter()
        {
            return m_GetAwaiter();
        }

        public void Dispose()
        {
            m_DisposeHandler.Invoke();
            m_ProgressSource.Dispose();
        }
    }
}