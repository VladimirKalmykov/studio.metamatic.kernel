using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    /// <summary>
    /// Async presenter is a base class that help to manage async operations in a presenter logic.
    /// It suitable for cases when you need to initialize some resources and then async enable/disable.
    /// E.g. modal window. By call Enable() method, you can initialize modal window, fade in, and by call Disable() method, you can hide it and release resources.
    /// </summary>
    public abstract class AsyncPresenterLegacy: IPolyDisposable, IAbortable
    {
        internal SafeUniTaskCompletionSource DisableCompletionSource;
        internal bool IsBeingActivated;
        internal readonly Observable<bool> EnabledObservable = ParamLessPool<Observable<bool>>.GetShared();
        protected bool IsDisposed;
        protected event Action OnDispose;
        protected SignalSource EnablingCancellationSignalSource;
        protected Func<Func<UniTask>, UniTask> m_ActivationHook;
        
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
        
        /// <summary>
        /// Allows to set activation hook. Hook is a function that can be used to wrap the activation process.
        /// </summary>
        /// <param name="hook"></param>
        /// <returns></returns>
        public DisposableAction SetActivationHook(Func<Func<UniTask>, UniTask> hook)
        {
            m_ActivationHook = hook;
            return new DisposableAction(() => m_ActivationHook = null);
        }

        public virtual void Dispose()
        {
            Abort();
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

        public bool isAborted => IsDisposed;
        public void Abort()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            OnDispose?.Invoke();
            DisableCompletionSource.TrySetResult();
            ParamLessPool<Observable<bool>>.ReturnShared(EnabledObservable);
        }

        public DisposableAction AddAbortedListenerOnce(Action action)
        {
            OnDispose += action;
            return new DisposableAction(() => OnDispose -= action);
        }
    }
    public abstract class AsyncPresenter<TProperty>: AsyncPresenterLegacy
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
     
         private async UniTaskVoid AsyncEnablingChanged(bool enabled, Signal signal)
         {
             Console.Debug("AsyncPresenter", $"Enabling changed: {enabled}");
             if (enabled)
             {
                 if (!EnablingCancellationSignalSource.CanBeCancelled)
                 {
                     EnablingCancellationSignalSource = SignalSource.Create();
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
                     EnablingCancellationSignalSource.TryRaise();
                 }
             }
         }
         
         internal override void InternalActivate()
         {
             if (m_ActivationHook == null)
             {
                 InternalActivateWithHook().Forget();
             }
             else
             {
                 InternalActivateWithoutHook().Forget();
             }
         }

         /// <summary>
         /// Method implements activation without hook.
         /// </summary>
         private async UniTask InternalActivateWithoutHook()
         {
             try
             {
                 await OnActivate(Ready);
             }
             catch (OperationCanceledException)
             {
                 // Skip
             } catch (Exception e)
             {
                 Console.Error("AsyncPresenter", e);
             }
         }

         /// <summary>
         /// Method implements activation with hook. Hook is a function that can be used to wrap the activation process.
         /// </summary>
         private async UniTaskVoid InternalActivateWithHook()
         {
                try
                {
                    await m_ActivationHook.Invoke(InternalActivateWithoutHook);
                }
                catch (OperationCanceledException)
                {
                    // Skip
                } catch (Exception e)
                {
                    Console.Error("AsyncPresenter", e);
                }
         }
        
         protected abstract UniTask OnActivate(Func<TProperty, UniTask> ready);
         /// <summary>
         /// Allows to perform some async actions while the presenter is enabled.
         /// Enable cancellation signal will be triggered only when the presenter is asynchronously completed OnDisable method.
         /// </summary>
         /// <param name="property"></param>
         /// <param name="cancellationSignal"></param>
         /// <returns></returns>
         protected abstract UniTaskVoid WhileEnabled(TProperty property, Signal cancellationSignal);
         protected abstract UniTask OnEnable(TProperty property,
             Signal cancellationSignal);

         protected abstract UniTask OnDisable(TProperty property, Signal cancellationSignal);
    }
}