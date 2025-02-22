using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Metamatic.SaveSystem;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.AddressableAssets;

namespace Metamatic.Framework.Workshop
{
    /// <summary>
    /// Base class for all game entities
    /// </summary>
    public abstract class Entity : AsyncBehaviour, ISavable
    {
        [Serializable]
        [Version("1.0.0")]
        public struct SaveData
        {
            public AssetReferenceT<EntityBlueprint> blueprint;
            public string id;
            public Vector3 position;
            public Quaternion rotation;
            public EntityComponent.SaveData[] ComponentsData;
            
            public SaveData(AssetReferenceT<EntityBlueprint> blueprint, string id, Vector3 position, Quaternion rotation)
            {
                this.blueprint = blueprint;
                this.id = id;
                this.position = position;
                this.rotation = rotation;
                ComponentsData = Array.Empty<EntityComponent.SaveData>();
            }
        }
        public AssetReferenceT<EntityBlueprint> blueprint { get; private set; }
        public string id { get; private set; }
        private event Action OnEntityResetEvent;
        private event Action OnEntityUpdateEvent;
        public readonly ObservableList<EntityComponent> Components = new ObservableList<EntityComponent>();
        private readonly Dictionary<Type, EntityComponent> m_RegisteredComponents = new Dictionary<Type, EntityComponent>();
        private readonly Dictionary<Type, EntityAbility> m_RegisteredAbilities = new Dictionary<Type, EntityAbility>();
        
        public string Initialize(AssetReferenceT<EntityBlueprint> blueprint, string id)
        {
            if (id == null)
            {
                id = Guid.NewGuid().ToString();
            }
            this.id = id;
            this.blueprint = blueprint;
            return this.id;
        }
        
        public T ResolveComponent<T>() where T : EntityComponent
        {
            if (m_RegisteredComponents.TryGetValue(typeof(T), out var component))
            {
                return (T) component;
            }

            return null;
        }
        
        public T ResolveAbility<T>() where T : EntityAbility
        {
            if (m_RegisteredAbilities.TryGetValue(typeof(T), out var ability))
            {
                return (T) ability;
            }

            return null;
        }
        
        public bool TryResolveComponent<T>(out T component) where T : EntityComponent
        {
            if (m_RegisteredComponents.TryGetValue(typeof(T), out var entityComponent))
            {
                component = (T) entityComponent;
                return true;
            }

            component = null;
            return false;
        }
        
        public bool TryResolveAbility<T>(out T ability) where T : EntityAbility
        {
            if (m_RegisteredAbilities.TryGetValue(typeof(T), out var entityAbility))
            {
                ability = (T) entityAbility;
                return true;
            }

            ability = null;
            return false;
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
        
        public void RegisterAbility(EntityAbility ability)
        {
            if (!m_RegisteredAbilities.ContainsKey(ability.GetType()))
            {
                m_RegisteredAbilities.Add(ability.GetType(), ability);
            }
        }
        
        public void UnregisterAbility(EntityAbility ability)
        {
            if (!m_RegisteredAbilities.ContainsKey(ability.GetType()))
            {
                throw new InvalidOperationException($"Ability of type {ability.GetType().Name} is not registered");
            }
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

        public object Save()
        {
            var entityTransform = transform;
            var componentsData = new EntityComponent.SaveData[Components.Count];
            var saveData = new SaveData(blueprint, id, entityTransform.position, entityTransform.rotation)
            {
                ComponentsData = componentsData
            };
            var j = 0;
            foreach (var component in Components)
            {
                componentsData[j] = (EntityComponent.SaveData)component.Save();
                j++;
            }

            return saveData;
        }

        public UniTask Load(object data)
        {
            var entityData = (SaveData) data;
            var thisGameObject = gameObject;
            foreach (var componentSaveData in entityData.ComponentsData)
            {
                var component = thisGameObject.GetOrAddComponent<EntityComponent>(componentSaveData.ComponentType);
                component.Load(componentSaveData);
            }
            return UniTask.CompletedTask;
        }
    }

}