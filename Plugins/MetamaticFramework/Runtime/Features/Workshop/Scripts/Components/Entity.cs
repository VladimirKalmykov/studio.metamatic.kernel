using System;
using Cysharp.Threading.Tasks;

namespace Metamatic.Framework.Workshop
{
    /// <summary>
    /// Base class for all game entities
    /// </summary>
    public abstract class Entity : AsyncBehaviour
    {
        private event Action OnEntityResetEvent;
        private event Action OnEntityUpdateEvent;
        public void ResetEntity()
        {
            OnEntityResetEvent?.Invoke();
        }
        
        protected override async UniTask Run(Ready ready, CancellationSignal signal)
        {
            using var announce = Dispatcher.global.Announce(this, false);
            await OnEntityRun(ready, signal);
        }

        protected abstract UniTask OnEntityRun(Ready ready, CancellationSignal signal);
        
        public DisposableAction SubscribeToEntityReset(Action handler)
        {
            OnEntityResetEvent += handler;
            return new DisposableAction(() => OnEntityResetEvent -= handler);
        }
        
        public DisposableAction SubscribeToEntityUpdate(Action handler)
        {
            OnEntityUpdateEvent += handler;
            return new DisposableAction(() => OnEntityUpdateEvent -= handler);
        }
        
        protected virtual void OnEntityReset()
        {
            /* Override this method to implement custom reset logic */
        }

        // TODO: Update entity according to its LOD level
        protected virtual void Update()
        {
            OnEntityUpdateEvent?.Invoke();
        }
    }

}