using System;

namespace Metamatic
{
    public abstract class DatabaseStore
    {
        public abstract void OnFrameEnd();
    }

    public class DatabaseStore<TState> : DatabaseStore
    {
        public delegate void StateChangeHandler(ref TState state, ref TState prevState);

        public delegate void StateModifier(ref TState state);
        public TState state;
        private TState m_NextState;
        private bool m_ShouldUpdate = false;
        private bool m_UpdateInProcess = false;
        private event StateModifier m_PostUpdateModifiying = delegate {};
       
        public event StateChangeHandler stateListeners;

        protected virtual void OnStateChange(ref TState state, ref TState prevState) {}

        protected virtual void OnInitState() {}

        public void SetState(StateModifier modifier) {
            if (m_UpdateInProcess)
            {
                m_PostUpdateModifiying += modifier;
            }
            else
            {
                modifier(ref m_NextState);
                ForceUpdateState();
            }
        }
        
        /// <summary>
        /// Return by ref next state for direct editing (fastest way to mofidy next state, use it inside cycles and frames)
        /// </summary>
        /// <returns>Next state</returns>
        public ref TState GetNextStateRef()
        {
            return ref m_NextState;
        }

        public DatabaseStore(): base()
        {
            OnInitState();
        }
        
        public DatabaseStore(TState defaultState): base()
        {
            state = defaultState;
            m_NextState = defaultState;
        }
        
        /// <summary>
        /// Force state engine to update state at the end of this frame
        /// </summary>
        public void ForceUpdateState()
        {
            m_ShouldUpdate = true;
            OnFrameEnd();
        }

        public Action SubscribeState(StateChangeHandler handler) {
            stateListeners += handler;
            handler(ref m_NextState, ref state);
            return () => {
                UnsubscribeState(handler);
            };
        }

        public void UnsubscribeState(StateChangeHandler handler) {
            stateListeners -= handler;
        }

        public override void OnFrameEnd()
        {
            if (!m_ShouldUpdate) return;
            m_ShouldUpdate = false;
            m_UpdateInProcess = true;
            stateListeners?.Invoke(ref m_NextState, ref state);
            
            OnStateChange(ref m_NextState, ref state);
            state = m_NextState;
            m_PostUpdateModifiying.Invoke(ref m_NextState);
            m_PostUpdateModifiying = delegate { };
            m_UpdateInProcess = false;
        }
    }
}