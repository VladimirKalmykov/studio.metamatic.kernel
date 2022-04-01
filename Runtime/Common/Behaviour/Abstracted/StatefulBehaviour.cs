namespace Metamatic {
    using System;
    using UnityEngine;

    public abstract class StatefulBehaviour<State>: MonoBehaviour, ISerializationCallbackReceiver
        where State : struct
    {
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
        public event Action<State, State> stateListeners;

        protected virtual void OnStateChange(in State state, in State prevState) {}

        protected virtual void OnInitState() {}

        public void SetState(System.Func<State, State> modifier) {
            state = modifier(stateful_NextState);
        }

        public void SetState(State nextState) {
            state = nextState;
        }

        public Action SubscribeState(Action<State, State> handler) {
            stateListeners += handler;
            handler(state, default);
            return () => {
                UnsubscribeState(handler);
            };
        }

        public void UnsubscribeState(Action<State, State> handler) {
            stateListeners -= handler;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() {}

        void ISerializationCallbackReceiver.OnAfterDeserialize() {
            OnInitState();
        }

        private void LateUpdate()
        {
            if (!stateful_ShouldUpdate) return;
            stateful_ShouldUpdate = false;
            var previousState = stateful_State;
            stateful_State = stateful_NextState;
            OnStateChange(stateful_State, previousState);
            stateListeners?.Invoke(stateful_State, previousState);
        }
    }
}