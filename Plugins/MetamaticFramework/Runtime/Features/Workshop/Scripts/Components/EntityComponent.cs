using Cysharp.Threading.Tasks;

namespace Metamatic.Framework.Workshop
{
    /// <summary>
    /// Entity component is a base class for all other components that are attached to an Entity. E.g. Health, Inventory, etc.
    /// </summary>
    public abstract class EntityComponent: AsyncBehaviour
    {
        protected override async UniTask Run(Ready ready, CancellationSignal signal)
        {
            var entity = GetComponent<Entity>();
            if (entity == null)
            {
                Console.Exception("EntityComponent", "EntityComponent requires an Entity component to be attached to the same GameObject.");
                return;
            }

            using var resetListener = entity.SubscribeToEntityReset(OnEntityReset);
            OnEntityRun(ready, signal);
            await signal;
        }

        protected abstract UniTask OnEntityRun(Ready ready, CancellationSignal signal);

        protected abstract void OnEntityReset();
    }
}