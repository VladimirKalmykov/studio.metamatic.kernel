using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    /// <summary>
    /// Tool allows to activate async operation when the reactivator is enabled. If async operation is completed, the reactivator will reactivate the async operation if it is enabled.
    /// </summary>
    public readonly struct Reactivator: IAsyncDisposableAbortable
    {
        public static Reactivator Create(Func<Reactivation, AsyncCancellation, UniTask> handler, bool enabled = false)
        {
            var instance = InternalTaskReactivator.Create(handler, enabled);
            return new Reactivator(instance);
        }
        private readonly InternalTaskReactivator m_Source;
        private readonly int m_Version;
        private InternalTaskReactivator source => m_Source == null || m_Source.version != m_Version ? null : m_Source;
        
        public Reactivator(InternalTaskReactivator source)
        {
            m_Source = source;
            m_Version = source.version;
        }
        
        public void Enable()
        {
            source?.Enable();
        }
        
        public void Disable()
        {
            source?.Disable();
        }

        public void Dispose()
        {
            var currentSource = source;
            if (currentSource != null)
            {
                InternalTaskReactivator.Return(currentSource);
            }
        }

        public UniTask DisposeAsync()
        {
            var currentSource = source;
            if (currentSource != null)
            {
                return currentSource.ReturnAsync();
            }

            return UniTask.CompletedTask;
        }

        public bool isAborted => source?.isAborted ?? true;
        public void Abort()
        {
            source?.Abort();
        }

        public DisposableAction AddAbortedListenerOnce(Action action)
        {
            return source?.AddAbortedListenerOnce(action) ?? DisposableAction.Empty;
        }
    }

    public readonly struct Reactivation
    {
        private readonly InternalTaskReactivator m_Reactivator;
        private readonly int m_Version;
        public bool enabled => GetReactivator()?.oEnabled.Value ?? false;
        public Reactivation(InternalTaskReactivator reactivator)
        {
            m_Reactivator = reactivator;
            m_Version = reactivator.version;
        }
        
        public DisposableAction WatchEnabled(Action<bool> action)
        {
            var reactivator = GetReactivator();
            if (reactivator == null)
            {
                return DisposableAction.Empty;
            }
            var disposeWatch = reactivator.oEnabled.Watch(action);
            var disposeListenSignal = reactivator.resetSignal.Register(() =>
            {
                disposeWatch.Dispose();
                disposeWatch = default;
            });
            
            return new DisposableAction(() =>
            {
                disposeWatch.Dispose();
                disposeListenSignal.Dispose();
            });
        }

        public UniTask WhenDisabled(Signal cancellationSignal)
        {
            var reactivator = GetReactivator();
            if (reactivator == null)
            {
                return UniTask.CompletedTask;
            }

            return reactivator.oEnabled.WaitUntilValueIs(false, cancellationSignal);
        }
        
        private InternalTaskReactivator GetReactivator()
        {
            return m_Reactivator.version == m_Version ? m_Reactivator : null;
        }
    }
    public class InternalTaskReactivator: IResettable, IAsyncReturnable
    {
        public static readonly ParamLessPool<InternalTaskReactivator> Pool = new ();
        public static InternalTaskReactivator Create(Func<Reactivation, AsyncCancellation, UniTask> handler, bool enabled = false)
        {
            var instance = Pool.Get();
            instance.Initialize(handler, enabled);
            return instance;
        }
        public static void Return(InternalTaskReactivator instance)
        {
            Pool.Return(instance);
        }
        private Func<Reactivation, AsyncCancellation, UniTask> m_OnTurnOn;
        private AsyncCancellationSource m_CancellationSource = default;
        private bool m_DefaultEnabled;
        private bool m_IsActivated = false;
        private UniTask m_ActivationTask = UniTask.CompletedTask;
        public Observable<bool> oEnabled { get; private set; } = ParamLessPool<Observable<bool>>.GetShared();
        public Signal resetSignal => m_CancellationSource.Signal;
        public int version { get; private set; }

        private void Initialize(Func<Reactivation, AsyncCancellation, UniTask> onTurnOn, bool enabled = false)
        {
            m_OnTurnOn = onTurnOn;
            m_CancellationSource = AsyncCancellationSource.Create();
            m_DefaultEnabled = enabled;
            if (enabled)
            {
                Enable();
            }
        }

        public void Enable()
        {
            oEnabled.Value = true;
            if (!m_IsActivated)
            {
                m_ActivationTask = Activate();
            }
        }
        public void Disable()
        {
            oEnabled.Value = false;
        }

        private async UniTask Activate()
        {
            m_IsActivated = true;
            try
            {
                if (m_OnTurnOn == null)
                {
                    return;
                }
                await m_OnTurnOn.Invoke(new Reactivation(this), m_CancellationSource.Cancellation);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
            catch (Exception e)
            {
                Console.Error("Reactivator", e);
            } finally {
                m_IsActivated = false;
            }
        }
        public void Reset()
        {
            unchecked
            {
                version++;
            }

            m_CancellationSource.Abort();
            m_IsActivated = false;
            oEnabled.Value = m_DefaultEnabled;
            m_OnTurnOn = null;
            OnAborted = null;
            isAborted = false;
        }

        private event Action OnAborted;
        public bool isAborted { get; private set; }
        public void Abort()
        {
            if (isAborted)
            {
                Console.Debug("Reactivator", "Already aborted (should debug stack trace)");
                return;
            }
            isAborted = true;
            OnAborted?.Invoke();
            ParamLessPool<Observable<bool>>.ReturnShared(oEnabled);
        }

        public DisposableAction AddAbortedListenerOnce(Action action)
        {
            OnAborted += action;
            return new DisposableAction(() => OnAborted -= action);
        }

        public bool isReturning { get; private set; }
        public async UniTask ReturnAsync()
        {
            isReturning = true;
            try
            {
                using var asyncCancellation = m_CancellationSource.TryCancelAsync();
                await m_ActivationTask;
            }
            finally
            {
                isReturning = false;
                Abort();
            }
        }
    }
}