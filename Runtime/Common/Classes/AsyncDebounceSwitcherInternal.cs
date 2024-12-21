using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public struct AsyncDebounceSwitcher: IDisposable
    {
        public static AsyncDebounceSwitcher Get(float delay, Func<bool, CancellationSignal, UniTaskVoid> onChange)
        {
            var instance = ParamLessPool<AsyncDebounceSwitcherInternal>.GetShared();
            instance.Initialize(delay, onChange);
            return new AsyncDebounceSwitcher(instance, () =>
            {
                ParamLessPool<AsyncDebounceSwitcherInternal>.ReturnShared(instance);
            });
        }
        private AsyncDebounceSwitcherInternal m_AsyncDebounceSwitcher;
        private Action m_OnDispose;

        public AsyncDebounceSwitcher(AsyncDebounceSwitcherInternal asyncDebounceSwitcher, Action onDispose)
        {
            m_AsyncDebounceSwitcher = asyncDebounceSwitcher;
            m_OnDispose = onDispose;
        }

        public void TurnOn()
        {
            m_AsyncDebounceSwitcher.TurnOn();
        }

        public void Dispose()
        {
            m_OnDispose();
        }
    }
    /// <summary>
    /// Allows to debounce switcher logic. When you call TurnOn, it will call OnOn action, and after delay, it will call OnOff action but if you call TurnOn again, it will reset the timer.
    /// </summary>
    public class AsyncDebounceSwitcherInternal: IDisposable, IResettable
    {
        private float m_Delay;
        private Func<bool, CancellationSignal, UniTaskVoid> m_OnChange;
        private float m_NextTimeOff = 0f;
        private bool m_IsTimerOn = false;
        private bool m_Initialized = false;
        /// <summary>
        /// Determines if the instance is reset (disposed or reset by pool)
        /// </summary>
        private bool m_IsReset = false;

        private CancellationSignalSource m_SwitchCancellationSignalSource = default;
        
        public AsyncDebounceSwitcherInternal()
        {
        }
        
        public AsyncDebounceSwitcherInternal(float delay, Func<bool, CancellationSignal, UniTaskVoid> onChange)
        {
            Initialize(delay, onChange);
        }

        public void Initialize(float delay, Func<bool, CancellationSignal, UniTaskVoid> onChange)
        {
            m_IsReset = false;
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
            m_SwitchCancellationSignalSource.TryCancel();
            m_SwitchCancellationSignalSource = CancellationSignalSource.Create();
            m_OnChange(true, m_SwitchCancellationSignalSource.Signal).Forget();
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
                    Time.time < m_NextTimeOff && !m_IsReset);
                m_SwitchCancellationSignalSource.TryCancel();
                m_SwitchCancellationSignalSource = CancellationSignalSource.Create();
                m_OnChange(false, m_SwitchCancellationSignalSource.Signal).Forget();
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
            m_SwitchCancellationSignalSource.TryCancel();
            m_IsReset = true;
            m_Delay = default;
            m_OnChange = default;
            m_NextTimeOff = default;
        }
    }
}