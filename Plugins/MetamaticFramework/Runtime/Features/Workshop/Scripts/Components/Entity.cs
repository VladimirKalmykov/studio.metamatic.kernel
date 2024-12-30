using System;
using System.Collections.Generic;
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
        public readonly ObservableList<EntityComponent> Components = new ObservableList<EntityComponent>();
        private readonly Dictionary<Type, EntityComponent> m_RegisteredComponents = new Dictionary<Type, EntityComponent>();
        public T ResolveComponent<T>() where T : EntityComponent
        {
            if (m_RegisteredComponents.TryGetValue(typeof(T), out var component))
            {
                return (T) component;
            }

            var newComponent = gameObject.AddComponent<T>();
            RegisterComponent(newComponent);
            return newComponent;
        }
        public void ResetEntity()
        {
            OnEntityResetEvent?.Invoke();
        }
        
        protected override async UniTask Run(Ready ready, Signal signal)
        {
            using var announce = Dispatcher.global.Announce(this, false);
            await OnEntityRun(ready, signal);
        }

        protected abstract UniTask OnEntityRun(Ready ready, Signal signal);
        
        public void RegisterComponent(EntityComponent component)
        {
            if (!m_RegisteredComponents.ContainsKey(component.GetType()))
            {
                m_RegisteredComponents.Add(component.GetType(), component);
                Components.Add(component);
            }
        }
        
        public void UnregisterComponent(EntityComponent component)
        {
            if (!m_RegisteredComponents.ContainsKey(component.GetType()))
            {
                throw new InvalidOperationException($"Component of type {component.GetType().Name} is not registered");
            }
            Components.Remove(component);
        }
        
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