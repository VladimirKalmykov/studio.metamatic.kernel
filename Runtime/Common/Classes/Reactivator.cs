using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    /// <summary>
    /// Tool allows to activate async operation when the reactivator is enabled. If async operation is completed, the reactivator will reactivate the async operation if it is enabled.
    /// </summary>
    public struct Reactivator: IDisposable
    {
        public static Reactivator Create(Func<Observable<bool>, Signal, UniTask> handler, bool enabled = false)
        {
            var instance = InternalTaskReactivator.Create(handler, enabled);
            return new Reactivator(instance);
        }
        private InternalTaskReactivator m_Source;
        private int m_Version;
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
    public class InternalTaskReactivator: IResettable, IDisposable
    {
        public static readonly ParamLessPool<InternalTaskReactivator> Pool = new ();
        public static InternalTaskReactivator Create(Func<Observable<bool>, Signal, UniTask> handler, bool enabled = false)
        {
            var instance = Pool.Get();
            instance.Initialize(handler, enabled);
            return instance;
        }
        public static void Return(InternalTaskReactivator instance)
        {
            Pool.Return(instance);
        }
        private Func<Observable<bool>, Signal, UniTask> m_OnTurnOn;
        private SignalSource m_ResetSignalSource = default;
        private bool m_DefaultEnabled;
        private bool m_IsActivated = false;
        private Observable<bool> m_EnableObservable = ParamLessPool<Observable<bool>>.GetShared();
        public int version { get; private set; }

        private void Initialize(Func<Observable<bool>, Signal, UniTask> onTurnOn, bool enabled = false)
        {
            m_OnTurnOn = onTurnOn;
            m_ResetSignalSource = new SignalSource();
            m_DefaultEnabled = enabled;
            if (enabled)
            {
                Enable();
            }
        }

        public void Enable()
        {
            m_EnableObservable.Value = true;
            if (!m_IsActivated)
            {
                Activate().Forget();
            }
        }
        public void Disable()
        {
            m_EnableObservable.Value = false;
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
                await m_OnTurnOn.Invoke(m_EnableObservable, m_ResetSignalSource.Signal);
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
            m_EnableObservable.Value = m_DefaultEnabled;
            m_OnTurnOn = null;
        }

        public void Dispose()
        {
            ParamLessPool<Observable<bool>>.ReturnShared(m_EnableObservable);
        }
    }
}