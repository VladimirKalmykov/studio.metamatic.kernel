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
        
        protected virtual void OnEntityReset()
        {
            /* Override this method to implement custom reset logic */
        }
    }

}