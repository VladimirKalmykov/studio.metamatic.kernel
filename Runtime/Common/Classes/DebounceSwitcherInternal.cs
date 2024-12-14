using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public struct DebounceSwitcher: IDisposable
    {
        public static DebounceSwitcher Get(float delay, Action<bool> onChange)
        {
            var instance = ParamLessPool<DebounceSwitcherInternal>.GetShared();
            instance.Initialize(delay, onChange);
            return new DebounceSwitcher(instance, () =>
            {
                ParamLessPool<DebounceSwitcherInternal>.ReturnShared(instance);
            });
        }
        private DebounceSwitcherInternal m_DebounceSwitcher;
        private Action m_OnDispose;

        public DebounceSwitcher(DebounceSwitcherInternal debounceSwitcher, Action onDispose)
        {
            m_DebounceSwitcher = debounceSwitcher;
            m_OnDispose = onDispose;
        }

        public void TurnOn()
        {
            m_DebounceSwitcher.TurnOn();
        }

        public void Dispose()
        {
            m_OnDispose();
        }
    }
    /// <summary>
    /// Allows to debounce switcher logic. When you call TurnOn, it will call OnOn action, and after delay, it will call OnOff action but if you call TurnOn again, it will reset the timer.
    /// </summary>
    public class DebounceSwitcherInternal: IDisposable, IResettable
    {
        private float m_Delay;
        private Action<bool> m_OnChange;
        private float m_NextTimeOff = 0f;
        private bool m_IsTimerOn = false;
        private bool m_Initialized = false;
        private CancellationSignalSource m_CancellationSignalSource = CancellationSignalSource.Create();
        
        public DebounceSwitcherInternal()
        {
        }
        
        public DebounceSwitcherInternal(float delay, Action<bool> onChange)
        {
            Initialize(delay, onChange);
        }

        public void Initialize(float delay, Action<bool> onChange)
        {
            m_CancellationSignalSource = CancellationSignalSource.Create();
            m_Delay = delay;
            m_OnChange = onChange;
            m_Initialized = true;
        }

        public void TurnOn()
        {
            #if DEBUG
            if (!m_Initialized)
            {
                Console.Error("DebounceSwitcher", "Not initialized");
            }
            #endif
            m_OnChange(true);
            m_NextTimeOff = Time.time + m_Delay;
            if (!m_IsTimerOn)
            {
                DelayedOff().Forget();
            }
        }
        
        private async UniTaskVoid DelayedOff()
        {
            try
            {
                m_IsTimerOn = true;
                await UniTask.WaitWhile(() =>
                    Time.time < m_NextTimeOff && !m_CancellationSignalSource.IsCancellationRequested);
                m_OnChange(false);
            }
            finally
            {
                m_IsTimerOn = false;
            }
        }

        public void Dispose()
        {
            Reset();
        }

        public void Reset()
        {
            m_CancellationSignalSource.TryCancel();
            m_Delay = default;
            m_OnChange = default;
            m_NextTimeOff = default;
        }
    }
}