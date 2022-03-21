using System;

namespace Metamatic
{
    public abstract class InstantEvent
    {
        public event Action OnEmit;
        public void Emit()
        {
            OnEmit?.Invoke();
        }
        
        public void Emit(Action<InstantEvent> mutator)
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