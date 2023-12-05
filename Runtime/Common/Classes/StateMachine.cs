using System;

namespace Metamatic
{
    public abstract class StateMachine
    {
        public abstract void Apply();
    }

    public class StateMachine<TState> : StateMachine
    {
        public delegate void StateChangeHandler(ref TState state, ref TState prevState);

        public delegate void StateModifier(ref TState state);
        public TState prevState;
        public TState state;
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
                modifier(ref state);
                ForceUpdateState();
            }
        }
        
        /// <summary>
        /// Return by ref next state for direct editing (fastest way to mofidy next state, use it inside cycles and frames)
        /// </summary>
        /// <returns>Next state</returns>
        public ref TState GetNextStateRef()
        {
            return ref state;
        }

        public StateMachine(): base()
        {
            OnInitState();
        }
        
        public StateMachine(TState defaultState): base()
        {
            prevState = defaultState;
            state = defaultState;
        }
        
        /// <summary>
        /// Force state engine to update state at the end of this frame
        /// </summary>
        public void ForceUpdateState()
        {
            m_ShouldUpdate = true;
        }

        public Action SubscribeState(StateChangeHandler handler) {
            stateListeners += handler;
            handler(ref state, ref prevState);
            return () => {
                UnsubscribeState(handler);
            };
        }

        public void UnsubscribeState(StateChangeHandler handler) {
            stateListeners -= handler;
        }
        
        /**
         * Call this method in Update() of MonoBehaviour
         */
        public void UpdateTick()
        {
            if (!m_ShouldUpdate) return;
            m_ShouldUpdate = false;
            Apply();
        }
        
        /**
         * Force update state. Call this method if you'd wish to invoke state update in the middle of frame
         */
        public override void Apply()
        {
            m_UpdateInProcess = true;
            stateListeners?.Invoke(ref state, ref prevState);
            
            OnStateChange(ref state, ref prevState);
            prevState = state;
            m_PostUpdateModifiying.Invoke(ref state);
            m_PostUpdateModifiying = delegate { };
            m_UpdateInProcess = false;
        }
    }
}