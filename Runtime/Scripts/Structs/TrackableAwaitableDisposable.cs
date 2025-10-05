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
        public static TrackableAwaitableDisposable Create(Func<Action, ProgressSource, Signal, UniTask> handler, Signal externalCancellationSignal)
        {
            var progressSource = ProgressSource.Create();
            var signalSource = SignalSource.CreateLinked(externalCancellationSignal);
            var taskSource = SafeUniTaskCompletionSource.Create();
            var handle = handler(() =>
            {
                taskSource.TrySetResult();
            }, progressSource, signalSource.Signal);
            ProcessHandle(handle, progressSource, signalSource, taskSource).Forget();

            return new TrackableAwaitableDisposable(() => taskSource.Task.GetAwaiter(), signalSource.TryRaise, progressSource);
        }

        private static async UniTaskVoid ProcessHandle(UniTask handle, ProgressSource progressSource, SignalSource signalSource, SafeUniTaskCompletionSource taskSource)
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
                progressSource.Dispose();
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

        public void Dispose()
        {
            m_DisposeHandler.Invoke();
        }
    }
    
    public readonly struct TrackableAwaitableDisposable<T>: IDisposable, IObservableValue<float>
    {
        public static TrackableAwaitableDisposable<T> Create(Func<Action<T>, Action<float>, Signal, UniTask> handler, Signal externalCancellationSignal)
        {
            var observableFloat = ParamLessPool<Observable<float>>.GetShared();
            observableFloat.value = 0f;
            var signalSource = SignalSource.CreateLinked(externalCancellationSignal);
            var taskSource = SafeUniTaskCompletionSource<T>.Create();
            var handle = handler((value) =>
            {
                observableFloat.uniqValue = 1f;
                taskSource.TrySetResult(value);
            }, observableFloat.SetValue, signalSource.Signal);
            ProcessHandle(handle, observableFloat, signalSource, taskSource).Forget();

            return new TrackableAwaitableDisposable<T>(() => taskSource.Task.GetAwaiter(), signalSource.TryRaise, observableFloat);
        }
        
        private static async UniTaskVoid ProcessHandle(UniTask handle, Observable<float> observableFloat, SignalSource signalSource, SafeUniTaskCompletionSource<T> taskSource)
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
                ParamLessPool<Observable<float>>.ReturnShared(observableFloat);
                signalSource.Dispose();
            }
        }
        
        private readonly Func<UniTask<T>.Awaiter> m_GetAwaiter;
        private readonly Action m_DisposeHandler;
        private readonly IObservableValue<float> m_ObservableProgress;

        public float progress => m_ObservableProgress.value;

        public TrackableAwaitableDisposable(Func<UniTask<T>.Awaiter> getAwaiter, Action disposeHandler, IObservableValue<float> progress)
        {
            m_GetAwaiter = getAwaiter;
            m_DisposeHandler = disposeHandler;
            m_ObservableProgress = progress;
        }
        
        public UniTask<T>.Awaiter GetAwaiter()
        {
            return m_GetAwaiter();
        }

        public void Dispose()
        {
            m_DisposeHandler.Invoke();
        }

        public IObservableValue<TOut> Select<TOut>(Func<float, TOut> selector, bool compare = false, Func<TOut, TOut, bool> comparator = null)
        {
            return m_ObservableProgress.Select(selector, compare, comparator);
        }

        public float value => m_ObservableProgress.value;
        public DisposableAction Watch(Action<float> handler, bool skipInitial = false, Func<float, bool> validator = null)
        {
            return m_ObservableProgress.Watch(handler, skipInitial, validator);
        }
    }
}