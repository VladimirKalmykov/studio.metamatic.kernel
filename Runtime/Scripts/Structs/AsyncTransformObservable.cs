using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public interface IDisposableObservableWithPending<T>: IObservableValue<T>, IDisposable
    {
        public IDisposable WatchPending(Action<bool> handler);
    }
    public readonly struct AsyncTransformObservable<TInput, TOutput>: IDisposable, IDisposableObservableWithPending<TOutput>
    {
        private static readonly Dictionary<int, (ObservableNullable<TOutput> outputObservable, IDisposable releaseWatch)> Props = new ();
        private static readonly Dictionary<int, SignalSource> SignalSources = new ();
        
        
        private readonly int m_Uuid;
        public readonly Progress progress;
        
        public AsyncTransformObservable(Func<TInput, Action<TOutput>, Signal, UniTask> handler,
            IObservableValue<TInput> observableInput)
        {
            var uuid = this.m_Uuid = IndexPool.Shared.Get();
            var outputObservable = ParamLessPool<ObservableNullable<TOutput>>.GetShared();
            var progressSource = ProgressSource.Create();
            progress = progressSource.Progress;
            SignalSource signalSource = default;
            var releaseWatch = observableInput.Watch(WatchHandler);
            var disposer = new CompositeDisposable(releaseWatch, progressSource);
            Props[uuid] = (outputObservable, disposer);

            void WatchHandler(TInput nextInput)
            {
                signalSource.Dispose();
                signalSource = SignalSource.Create();
                SignalSources[uuid] = signalSource;
                var record = progressSource.CreateStep();
                PerformHandler(handler, nextInput, outputObservable, record, signalSource).Forget();
            }
        }
        
        private static async UniTaskVoid PerformHandler(
            Func<TInput, Action<TOutput>, Signal, UniTask> handler,
            TInput nextInput,
            ObservableNullable<TOutput> outputObservable,
            IDisposable record,
            SignalSource signalSource)
        {
            try
            {
                await handler(nextInput, outputObservable.SetValidValue, signalSource.Signal);
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
            finally
            {
                record.Dispose();
            }
        }

        public void Dispose()
        {
            if (Props.Remove(this.m_Uuid, out var prop ))
            {
                
                ParamLessPool<ObservableNullable<TOutput>>.ReturnShared(prop.outputObservable);
                prop.releaseWatch.Dispose();
            }
            
            if (SignalSources.Remove(this.m_Uuid, out var signalSource))
            {
                signalSource.Dispose();
            }
            
            
            IndexPool.Shared.Release(m_Uuid);
        }

        public TOutput value => Props.TryGetValue(this.m_Uuid, out var prop) ? prop.outputObservable.value : default;

        public DisposableAction Watch(Action<TOutput> handler, bool skipInitial = false, Func<TOutput, bool> validator = null)
        {
            if (Props.TryGetValue(this.m_Uuid, out var prop))
            {
                var (oo, releaseWatch) = prop;
                return oo.Watch(Handler, skipInitial, validator);
            }
            return default;

            void Handler(TOutput nextInput, bool hasValue)
            {
                if (!hasValue) return;
                handler(nextInput);
            }
        }
        
        public IDisposable WatchPending(Action<bool> handler)
        {
            return progress.WatchPending(handler);
        }
        
        public UniTask<TOutput> WhenHasValue(Signal cancellationSignal)
        {
            if (Props.TryGetValue(this.m_Uuid, out var prop))
            {
                var (oo, _) = prop;
                return oo.WhenHasValue(cancellationSignal);
            }
            return UniTask.FromResult(default(TOutput));
        }
    }
}