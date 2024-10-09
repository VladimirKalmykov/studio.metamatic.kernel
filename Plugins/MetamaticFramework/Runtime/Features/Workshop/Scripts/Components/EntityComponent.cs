using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Framework.Workshop
{
    /// <summary>
    /// Entity component is a base class for all other components that are attached to an Entity. E.g. Health, Inventory, etc.
    /// </summary>
    public abstract class EntityComponent: AsyncBehaviour
    {
        private float m_LastTimeUpdate;
        #region Mono

        private void Start()
        {
            m_LastTimeUpdate = Time.time;
        }
        #endregion
        protected override async UniTask Run(Ready ready, CancellationSignal signal)
        {
            var entity = GetComponent<Entity>();
            if (entity == null)
            {
                Console.Exception("EntityComponent", "EntityComponent requires an Entity component to be attached to the same GameObject.");
                return;
            }

            using var resetListener = entity.SubscribeToEntityReset(OnEntityReset);
            using var updateListener = entity.SubscribeToEntityUpdate(InternalOnEntityUpdate);
            OnEntityRun(ready, signal);
            await signal;
        }

        private void InternalOnEntityUpdate()
        {
            var deltaTime = Time.time - m_LastTimeUpdate;
            m_LastTimeUpdate = Time.time;
            OnEntityUpdate(deltaTime);
        }

        protected abstract UniTask OnEntityRun(Ready ready, CancellationSignal signal);

        protected abstract void OnEntityReset();
        protected abstract void OnEntityUpdate(float deltaTime);
    }
}