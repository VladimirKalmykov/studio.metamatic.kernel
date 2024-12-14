using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    /// <summary>
    /// Async presenter is a base class that help to manage async operations in a presenter logic.
    /// It suitable for cases when you need to initialize some resources and then async enable/disable.
    /// E.g. modal window. By call Enable() method, you can initialize modal window, fade in, and by call Disable() method, you can hide it and release resources.
    /// </summary>
    public abstract class AsyncPresenter: IDisposable, IUniTaskAsyncDisposable
    {
        internal SafeUniTaskCompletionSource DisableCompletionSource;
        internal bool IsBeingActivated;
        internal readonly Observable<bool> EnabledObservable = ParamLessPool<Observable<bool>>.GetShared();
        protected bool IsDisposed;
        protected event Action OnDispose;
        protected CancellationSignalSource EnablingCancellationSignalSource;
        
        public void Enable()
        {
            EnabledObservable.SetUniqValue(true);
            if (IsBeingActivated) return;
            IsBeingActivated = true;
            InternalActivate();
        }

        internal abstract void InternalActivate();
     
        public void Disable()
        {
            EnabledObservable.SetUniqValue(false);
        }

        public virtual void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            OnDispose?.Invoke();
            DisableCompletionSource.TrySetResult();
            ParamLessPool<Observable<bool>>.ReturnShared(EnabledObservable);
        }
        
        public async UniTask DisposeAsync()
        {
            if (IsDisposed) return;
            Disable();
            IsDisposed = true;
            OnDispose?.Invoke();
            if (DisableCompletionSource.isAlive)
            {
                await DisableCompletionSource.Task;
            }
        }
    }
    public abstract class AsyncPresenter<TProperty>: AsyncPresenter
    {
         private TProperty m_Property;

         ~AsyncPresenter()
         {
             Console.Error("AsyncPresenter", "Is not disposed");
             Dispose();
         }
     
         private async UniTask Ready(TProperty property)
         {

             try
             {
                 m_Property = property;
                 if (!DisableCompletionSource.isAlive)
                 {
                     DisableCompletionSource = SafeUniTaskCompletionSource.Create();
                 }
                 Console.Debug("AsyncPresenter", "Ready");
                 using var watchEnabled = EnabledObservable.WatchAsync(AsyncEnablingChanged);
                 await DisableCompletionSource.Task;
                 IsBeingActivated = false;
                 Console.Debug("AsyncPresenter", "Released");
             } catch (Exception e)
             {
                 Console.Error("AsyncPresenter", e);
             }
         }
     
         private async UniTaskVoid AsyncEnablingChanged(bool enabled, CancellationSignal signal)
         {
             Console.Debug("AsyncPresenter", $"Enabling changed: {enabled}");
             if (enabled)
             {
                 if (!EnablingCancellationSignalSource.CanBeCancelled)
                 {
                     EnablingCancellationSignalSource = CancellationSignalSource.Create();
                 }
                 
                 WhileEnabled(m_Property, EnablingCancellationSignalSource.Signal).Forget();
                 await OnEnable(m_Property, signal);
             }
             else
             {
                 using var registration = signal.Register(() =>
                 {
                        Console.Debug("AsyncPresenter", "Disabled just has been cancelled");
                 });
                 try
                 {
                     await OnDisable(m_Property, signal);
                     if (!signal.IsCancellationRequested)
                     {
                         Console.Debug("AsyncPresenter", "Disable completed");
                         DisableCompletionSource.TrySetResult();
                     }
                     else
                     {
                         Console.Debug("AsyncPresenter", "Disable not completed, because hb cancelled");
                     }
                 }
                 finally
                 {
                     EnablingCancellationSignalSource.TryCancel();
                 }
             }
         }
         
         internal override void InternalActivate()
         {
             OnActivate(Ready).Forget();
         }
        
         protected abstract UniTaskVoid OnActivate(Func<TProperty, UniTask> ready);
         /// <summary>
         /// Allows to perform some async actions while the presenter is enabled.
         /// Enable cancellation signal will be triggered only when the presenter is asynchronously completed OnDisable method.
         /// </summary>
         /// <param name="property"></param>
         /// <param name="cancellationSignal"></param>
         /// <returns></returns>
         protected abstract UniTaskVoid WhileEnabled(TProperty property, CancellationSignal cancellationSignal);
         protected abstract UniTask OnEnable(TProperty property,
             CancellationSignal cancellationSignal);

         protected abstract UniTask OnDisable(TProperty property, CancellationSignal cancellationSignal);
    }
}