using System;

namespace Metamatic
{
    public class AggressiveStateMachine<State>
    {
        private State state;
        public delegate void StateModifier(ref State state);
        public event StateModifier stateListeners;

        public void SetState(StateModifier modifier)
        {
            modifier(ref state);
            stateListeners?.Invoke(ref state);
        }
    
        public Action Subscribe(StateModifier handler) {
            stateListeners += handler;
            handler(ref state);
            return () => {
                Unsubscribe(handler);
            };
        }

        public void Unsubscribe(StateModifier handler) {
            stateListeners -= handler;
        }
    }
}