using System;

namespace Metamatic
{
    public class InstantEvent
    {
        public event Action OnEmit;
        public void Emit()
        {
            OnEmit?.Invoke();
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