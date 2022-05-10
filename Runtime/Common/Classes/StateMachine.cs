using System;
using PlasticGui.WorkspaceWindow;
using UnityEngine;

namespace Metamatic
{
    public class StateMachine
    {
        
    }

    public class StateMechine<State> : StateMachine
    {
        public delegate void StateChangeHandler(ref State state, ref State prevState);

        public delegate void StateModifier(ref State state);
        private State m_State;
        private State m_NextState;
        private bool m_ShouldUpdate = false;
        public State state {
            get => m_State;
            protected set
            {
                m_NextState = value;
                m_ShouldUpdate = true;
            }
        }
        public event StateChangeHandler stateListeners;

        protected virtual void OnStateChange(ref State state, ref State prevState) {}

        protected virtual void OnInitState() {}

        public void SetState(StateModifier modifier) {
            modifier(ref m_NextState);
            ForceUpdateState();
        }
        
        /// <summary>
        /// Return by ref next state for direct editing (fastest way to mofidy next state, use it inside cycles and frames)
        /// </summary>
        /// <returns>Next state</returns>
        public ref State GetNextStateRef()
        {
            return ref m_NextState;
        }

        public StateMechine(): base()
        {
            OnInitState();
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
            handler(ref m_State, ref m_State);
            return () => {
                UnsubscribeState(handler);
            };
        }

        public void UnsubscribeState(StateChangeHandler handler) {
            stateListeners -= handler;
        }

        private void OnFrameEnd()
        {
            if (!m_ShouldUpdate) return;
            m_ShouldUpdate = false;
            stateListeners?.Invoke(ref m_NextState, ref m_State);
            OnStateChange(ref m_NextState, ref m_State);
            m_State = m_NextState;
        }
    }
}