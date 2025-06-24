using System;
using Cysharp.Threading.Tasks;
using Metamatic;
using Console = Metamatic.Console;

namespace Solutions.Runner
{
    public abstract class CustomBehaviour: IDisposable
    {
        private bool m_Enabled;
        private SignalSource m_DisableSignalSource = default;

        protected CustomBehaviour(bool autoEnable = true)
        {
            Enable();
        }

        public void Enable()
        {
            if (m_Enabled) return;
            m_Enabled = true;
            m_DisableSignalSource.Dispose();
            m_DisableSignalSource = SignalSource.Create();
            EnableAsync().Forget();
        }

        public void Disable()
        {
            if (!m_Enabled) return;
            m_DisableSignalSource.TryRaise();
        }

        private async UniTaskVoid EnableAsync()
        {
            try
            {
                var signal = m_DisableSignalSource.Signal;
                await UniTaskEventLoop.Shared.Yield(signal);
                await OnEnableAsync(m_DisableSignalSource.Signal);
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
            catch (Exception ex)
            {
                Console.Error("CustomBehaviour", ex);
            }
        }
        protected abstract UniTask OnEnableAsync(Signal disableSignal);
        public void Dispose()
        {
            Disable();
        }
    }
}