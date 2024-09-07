using System;

namespace Metamatic
{
    /// <summary>
    /// Base class for all game entities
    /// </summary>
    public abstract class Entity : AsyncBehaviour
    {
        private event Action OnEntityResetEvent;
        public void ResetEntity()
        {
            OnEntityResetEvent?.Invoke();
        }
        
        public DisposableAction SubscribeToEntityReset(Action handler)
        {
            OnEntityResetEvent += handler;
            return new DisposableAction(() => OnEntityResetEvent -= handler);
        }
        
        protected virtual void OnEntityReset()
        {
            /* Override this method to implement custom reset logic */
        }
    }

}