using System;
using Cysharp.Threading.Tasks;
using Metamatic;

namespace Metamatic.Framework
{
    public abstract class VirtualBehaviour: IDisposable
    {
        private SignalSource m_DisposeSignalSource = default;
        private SignalSource m_DisableSignalSource = default;
        private bool m_Enabled = false;

        public bool enabled
        {
            set
            {
                if (value == m_Enabled)
                {
                    return;
                }

                if (value)
                {
                    Enable(); 
                }
                else
                {
                    Disable();
                }
            }
        }

        public void Awake()
        {
            m_DisposeSignalSource = SignalSource.Create();
            OnAwakeAsync(m_DisposeSignalSource.Signal).Forget();
        }
        
        private void Enable()
        {
            m_DisableSignalSource = SignalSource.Create();
            OnEnableAsync(m_DisableSignalSource.Signal).Forget();
        }

        private void Disable()
        {
            m_DisableSignalSource.TryRaise();
        }

        public virtual void Destroy()
        {
            
        }

        public virtual UniTaskVoid OnEnableAsync(Signal cancellationSignal)
        {
            return new UniTaskVoid();
        }
        public virtual UniTaskVoid OnAwakeAsync(Signal cancellationSignal)
        {
            return new UniTaskVoid();
        }
        public void Dispose()
        {
            m_DisableSignalSource.TryRaise();
            m_DisposeSignalSource.TryRaise();
        }
    }
}