using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    /// <summary>
    /// Allows you to create a switch that turns off automatically after the timer expires.
    /// </summary>
    /// <example>
    /// using var autoOffSwitch = AutoOffSwitch.Create(1f, () => Debug.Log("Turned on"), () => Debug.Log("Turned off"), () => Debug.Log("Aborted"));
    /// autoOffSwitch.TurnOn(); // Turned on, after 1 second Turned off automatically. And if you call Dispose, it will call Aborted.
    /// </example>
    public readonly struct AutoOffSwitch: IDisposable
    {
        public static AutoOffSwitch Create(float duration, Action onTurnOn, Action onTurnOff, Action onAbort)
        {
            var instance = AutoOffSwitchInternal.Get();
            instance.Initialize(duration, onTurnOn, onTurnOff, onAbort);
            return new AutoOffSwitch(instance);
        }
        
        public static AutoOffSwitch Create(float duration, Func<AsyncCancellation, UniTaskVoid> onTurnOn)
        {
            var asyncCancellationSource = AsyncCancellationSource.Create();
            return Create(duration, () =>
            {
                onTurnOn(asyncCancellationSource.Cancellation).Forget();
            }, () =>
            {
                asyncCancellationSource.TryCancelAsync(20f);
            }, () =>
            {
                asyncCancellationSource.Dispose();
            });
        }
        
        private readonly AutoOffSwitchInternal m_Source;
        private readonly int m_Version;

        public AutoOffSwitch(AutoOffSwitchInternal source)
        {
            m_Source = source;
            m_Version = source.version;
        }
        
        public void TurnOn()
        {
            source?.TurnOn();
        }
        
        public void TurnOff()
        {
            source?.TurnOff();
        }
        
        private AutoOffSwitchInternal source
        {
            get
            {
                return m_Source == null || m_Source.version != m_Version ? null : m_Source;
            }
        }

        public void Dispose()
        {
            var currentSource = source;
            if (currentSource != null)
            {
                AutoOffSwitchInternal.Return(currentSource);
            }
        }
    }
    
    public class AutoOffSwitchInternal: IResettable
    {
        private static readonly ParamLessPool<AutoOffSwitchInternal> Pool = new ();

        public static AutoOffSwitchInternal Get()
        {
            return Pool.Get();
        }
        public static void Return(AutoOffSwitchInternal instance)
        {
            Pool.Return(instance);
        }
        private float m_Duration;
        private Action m_OnTurnOn;
        private Action m_OnTurnOff;
        private Action m_OnReset;
        private SignalSource m_BreakTimerSignalSource = default;
        public int version { get; private set; }
        public void Initialize(float duration, Action onTurnOn, Action onTurnOff, Action onReset)
        {
            m_Duration = duration;
            m_OnTurnOn = onTurnOn;
            m_OnTurnOff = onTurnOff;
            m_OnReset = onReset;
        }
        private bool m_IsOn = false;
        public void TurnOn()
        {
            m_BreakTimerSignalSource.TryRaise();
            var isTurningOn = !m_IsOn;
            m_IsOn = true;
            StartTurnOffTimer().Forget();
            if (isTurningOn)
            {
                m_OnTurnOn?.Invoke();
            }
        }
        
        /// <summary>
        /// Immediately turns off the switch.
        /// </summary>
        public void TurnOff()
        {
            if (!m_IsOn)
            {
                return;
            }
            m_BreakTimerSignalSource.TryRaise();
            m_IsOn = false;
            m_OnTurnOff?.Invoke();
        }
        
        private async UniTaskVoid StartTurnOffTimer()
        {
            var timerId = unchecked((int)DateTime.Now.Ticks);
            m_BreakTimerSignalSource = SignalSource.Create();
            try
            {
                await PlayerLoop.Delay(m_Duration, PlayerLoopTiming.LastPostLateUpdate,
                    m_BreakTimerSignalSource.Signal);
                TurnOff();
            }
            catch (OperationCanceledException)
            {
                // Skip
            }
            catch (Exception e)
            {
                Console.Error("AutoOffSwitch", e);
            }
        }

        public void Reset()
        {
            unchecked
            {
                version++;
            }
            TurnOff();
            m_OnReset?.Invoke();
            m_OnTurnOn = null;
            m_OnTurnOff = null;
            m_OnReset = null;
        }
    }
}