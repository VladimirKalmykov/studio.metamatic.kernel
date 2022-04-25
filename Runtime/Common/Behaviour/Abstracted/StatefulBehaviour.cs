namespace Metamatic {
    using System;
    using UnityEngine;

    public abstract class StatefulBehaviour : MonoBehaviour
    {
        protected event Action OnStatePreLateUpdate;
        public class Debouncer
        {
            private float m_Duration = 1f;
            private float m_StartTime;
            private Action m_Action;
            private bool m_IsThrottle = false;
            public event Action OnDispose;

            private StatefulBehaviour m_Host;

            public Debouncer(StatefulBehaviour statefulBehaviour)
            {
                m_Host = statefulBehaviour;
            }

            public void Debounce(Action action, float duration, bool throttle = false)
            {
                if (!throttle || m_StartTime == 0f)
                {
                    m_StartTime = Time.time;
                }
                m_IsThrottle = throttle;
                m_Duration = duration;
                m_Action = action;
                m_Host.OnStatePreLateUpdate += Tick;
            }

            public void Tick()
            {
                if (m_StartTime == 0f || Time.time - m_StartTime < m_Duration) return;
                m_StartTime = 0f;
                Apply();
            }

            private void Apply()
            {
                m_StartTime = 0f;
                m_Host.OnStatePreLateUpdate -= Tick;
                m_Action.Invoke();
            }

            public void Dispose()
            {
                m_Host.OnStatePreLateUpdate -= Tick;
                m_StartTime = 0f;
                m_Action = null;
            }
        }

        public Debouncer CreateDebouncer()
        {
            var valueStabilizer = new Debouncer(this);

            return valueStabilizer;
        }

        protected void StatePreLateUpdate()
        {
            OnStatePreLateUpdate?.Invoke();
        }
    }
    
    public abstract class StatefulBehaviour<State>: StatefulBehaviour, ISerializationCallbackReceiver
        where State : struct
    {
        public delegate void StateChangeHandler(ref State state, ref State prevState);

        public delegate void StateModifier(ref State state);
        private State stateful_State;
        private State stateful_NextState;
        private bool stateful_ShouldUpdate = false;
        public State state {
            get { return stateful_State; }
            protected set
            {
                stateful_NextState = value;
                stateful_ShouldUpdate = true;
            }
        }
        public event StateChangeHandler stateListeners;

        protected virtual void OnStateChange(ref State state, ref State prevState) {}

        protected virtual void OnInitState() {}

        public void SetState(StateModifier modifier) {
            modifier(ref stateful_NextState);
            ForceUpdateState();
        }

        /*public void SetState(in State nextState) {
            stateful_NextState = nextState;
            stateful_ShouldUpdate = true;
        }*/
        
        /// <summary>
        /// Return by ref next state for direct editing (fastest way to mofidy next state, use it inside cycles and frames)
        /// </summary>
        /// <returns>Next state</returns>
        public ref State GetNextStateRef()
        {
            return ref stateful_NextState;
        }
        
        /// <summary>
        /// Force state engine to update state at the end of this frame
        /// </summary>
        public void ForceUpdateState()
        {
            stateful_ShouldUpdate = true;
        }

        public Action SubscribeState(StateChangeHandler handler) {
            stateListeners += handler;
            handler(ref stateful_State, ref stateful_State);
            return () => {
                UnsubscribeState(handler);
            };
        }

        public void UnsubscribeState(StateChangeHandler handler) {
            stateListeners -= handler;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() {}

        void ISerializationCallbackReceiver.OnAfterDeserialize() {
            OnInitState();
        }

        private void LateUpdate()
        {
            StatePreLateUpdate();
            if (!stateful_ShouldUpdate) return;
            stateful_ShouldUpdate = false;
            stateListeners?.Invoke(ref stateful_NextState, ref stateful_State);
            OnStateChange(ref stateful_NextState, ref stateful_State);
            stateful_State = stateful_NextState;
            
        }
    }
}