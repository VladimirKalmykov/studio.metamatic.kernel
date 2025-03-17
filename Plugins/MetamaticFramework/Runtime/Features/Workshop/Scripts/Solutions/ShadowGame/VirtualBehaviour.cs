using System;

namespace Metamatic.Framework
{
    public abstract class VirtualBehaviour: IDisposable
    {
        private bool m_Enabled = false;

        public bool enabled
        {
            set
            {
                if (value == m_Enabled)
                {
                    return;
                }

                m_Enabled = value;
                if (value)
                {
                    Enable(); 
                }
                else
                {
                    Disable();
                }
            }
            
            get => m_Enabled;
        }
        
        internal void InternalAwake()
        {
            Awake();
        }

        protected virtual void Awake()
        {
            
        }
        
        protected virtual void Enable()
        {
            
        }

        protected virtual void Disable()
        {
            
        }

        protected virtual void Destroy()
        {
            
        }
        public void Dispose()
        {
            Destroy();
        }
    }
}