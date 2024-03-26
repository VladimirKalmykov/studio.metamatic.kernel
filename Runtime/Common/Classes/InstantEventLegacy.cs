using System;

namespace Metamatic
{
    public abstract class InstantEventLegacy
    {
        public object emitter;
        public event Action OnEmit;
        public void Emit(object emitter = null)
        {
            this.emitter = emitter;
            OnEmit?.Invoke();
        }
        
        public void Emit(Action<InstantEventLegacy> mutator)
        {
            mutator(this);
            Emit();
        }

        public object Persist()
        {
            return this.MemberwiseClone();
        }

        public void AddListener(Action handler)
        {
            OnEmit += handler;
        }

        public void RemoveListener(Action handler)
        {
            OnEmit -= handler;
        }
    }
}