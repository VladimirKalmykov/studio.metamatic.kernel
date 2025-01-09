using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    /// <summary>
    /// Tool allows to activate async operation when the reactivator is enabled. If async operation is completed, the reactivator will reactivate the async operation if it is enabled.
    /// </summary>
    public readonly struct Reactivator: IDisposable
    {
        public static Reactivator Create(Func<Reactivation, Signal, UniTask> handler, bool enabled = false)
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
    public class InternalTaskReactivator: IResettable, IDisposable
    {
        public static readonly ParamLessPool<InternalTaskReactivator> Pool = new ();
        public static InternalTaskReactivator Create(Func<Reactivation, Signal, UniTask> handler, bool enabled = false)
        {
            var instance = Pool.Get();
            instance.Initialize(handler, enabled);
            return instance;
        }
        public static void Return(InternalTaskReactivator instance)
        {
            Pool.Return(instance);
        }
        private Func<Reactivation, Signal, UniTask> m_OnTurnOn;
        private SignalSource m_ResetSignalSource = default;
        private bool m_DefaultEnabled;
        private bool m_IsActivated = false;
        public Observable<bool> oEnabled { get; private set; } = ParamLessPool<Observable<bool>>.GetShared();
        public Signal resetSignal => m_ResetSignalSource.Signal;
        public int version { get; private set; }

        private void Initialize(Func<Reactivation, Signal, UniTask> onTurnOn, bool enabled = false)
        {
            m_OnTurnOn = onTurnOn;
            m_ResetSignalSource = SignalSource.Create();
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
                Activate().Forget();
            }
        }
        public void Disable()
        {
            oEnabled.Value = false;
        }

        private async UniTaskVoid Activate()
        {
            m_IsActivated = true;
            if (m_OnTurnOn == null)
            {
                return;
            }

            try
            {
                await m_OnTurnOn.Invoke(new Reactivation(this), m_ResetSignalSource.Signal);
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
            m_ResetSignalSource.TryRaise();
            m_IsActivated = false;
            oEnabled.Value = m_DefaultEnabled;
            m_OnTurnOn = null;
        }

        public void Dispose()
        {
            ParamLessPool<Observable<bool>>.ReturnShared(oEnabled);
        }
    }
}