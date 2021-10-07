namespace Metamatic {
    using UnityEngine;

    public abstract class StatefulBehaviour<State>: MonoBehaviour, ISerializationCallbackReceiver
        where State : struct
    {
        private State stateful_State;
        private State stateful_PrevState;
        private bool stateful_ShouldUpdate = false;
        public State state {
            get { return stateful_State; }
            protected set {
                stateful_State = value;
                stateful_ShouldUpdate = true;
            }
        }

        protected virtual void OnStateChange(State state, State prevState) {}

        protected virtual void OnInitState() {}

        public void SetState(System.Func<State, State> modifier) {
            state = modifier(state);
        }

        public void SetState(State nextState) {
            state = nextState;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() {}

        void ISerializationCallbackReceiver.OnAfterDeserialize() {
            OnInitState();
        }

        void LateUpdate() {
            if (stateful_ShouldUpdate) {
                stateful_ShouldUpdate = false;
                OnStateChange(stateful_State, stateful_PrevState);
                stateful_PrevState = stateful_State;
            }
        }
    }
}