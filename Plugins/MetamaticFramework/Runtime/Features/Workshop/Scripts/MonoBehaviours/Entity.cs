using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Metamatic.SaveSystem;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Metamatic.Framework.Workshop
{
    /// <summary>
    /// Base class for all game entities
    /// </summary>
    public class Entity : AsyncBehaviour, ISavable
    {
        [Serializable]
        [Version("1.0.0")]
        public struct SaveData
        {
            public AssetReferenceT<EntityBlueprint> blueprint;
            public string id;
            public bool isStatic;
            public Vector3 position;
            public Quaternion rotation;
            public EntityComponent.SaveData[] ComponentsData;

            public SaveData(AssetReferenceT<EntityBlueprint> blueprint, string id, bool isStatic, Vector3 position,
                Quaternion rotation)
            {
                this.blueprint = blueprint;
                this.id = id;
                this.isStatic = isStatic;
                this.position = position;
                this.rotation = rotation;
                ComponentsData = Array.Empty<EntityComponent.SaveData>();
            }
        }

        #region Serializable fields

        [Tooltip("Unique identifier of the entity")]
        [ReadOnly] public string id;

        #endregion

        private void GenerateUniqueId(bool force = false)
        {
            id = Guid.NewGuid().ToString();
        }

        public AssetReferenceT<EntityBlueprint> blueprint { get; private set; }

        private event Action OnEntityResetEvent;
        private event Action OnEntityUpdateEvent;
        public readonly ObservableList<EntityComponent> Components = new ObservableList<EntityComponent>();

        private readonly Dictionary<Type, EntityComponent> m_RegisteredComponents =
            new Dictionary<Type, EntityComponent>();

        private readonly Dictionary<Type, EntityAbility> m_RegisteredAbilities = new Dictionary<Type, EntityAbility>();

        /// <summary>
        /// Determines that entity was not spawned dynamically
        /// </summary>
        public bool isStatic { get; private set; }= true;

        #region Mono
        
        protected override void Awake()
        {
            base.Awake();
            Debug.Log("Объект находится в сцене: " + gameObject.scene.name);

#if UNITY_EDITOR
            if (EditorUtility.IsPersistent(gameObject))
            {
                Debug.Log("Это префаб-ассет (не инстанцированный объект в сцене)");
            }
            else
            {
                Debug.Log("Это экземпляр, находящийся в сцене");
            }
#endif
        }

        // TODO: Update entity according to its LOD level
        protected virtual void Update()
        {
            OnEntityUpdateEvent?.Invoke();
        }

#if UNITY_EDITOR
        // Метод вызывается при изменениях в редакторе
        protected override void OnValidate()
        {
            // EditorUtility.IsPersistent(gameObject)
            if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
            {
                /* Erase Id for prefab assets */
                id = null;
                EditorUtility.SetDirty(this);
            } else if (string.IsNullOrEmpty(id))
            {
                GenerateUniqueId();
                // Отмечаем объект как изменённый, чтобы изменения сохранились
                EditorUtility.SetDirty(this);
            }
        }
#endif

        #endregion
        
        #region Mono

        private Token96Bit m_RegistrationToken;
        protected void Start()
        {
            m_RegistrationToken = EntitiesService.TryRegisterEntity(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            EntitiesService.TryUnregisterEntity(m_RegistrationToken);
        }
        #endregion

        public string Initialize(AssetReferenceT<EntityBlueprint> blueprint, string id, bool isStatic = false)
        {
            if (id == null)
            {
                id = Guid.NewGuid().ToString();
            }

            this.id = id;
            this.blueprint = blueprint;
            this.isStatic = isStatic;
            return this.id;
        }

        public T ResolveComponent<T>() where T : EntityComponent
        {
            if (m_RegisteredComponents.TryGetValue(typeof(T), out var component))
            {
                return (T)component;
            }

            return null;
        }

        public T ResolveAbility<T>() where T : EntityAbility
        {
            if (m_RegisteredAbilities.TryGetValue(typeof(T), out var ability))
            {
                return (T)ability;
            }

            return null;
        }

        public bool TryResolveComponent<T>(out T component) where T : EntityComponent
        {
            if (m_RegisteredComponents.TryGetValue(typeof(T), out var entityComponent))
            {
                component = (T)entityComponent;
                return true;
            }

            component = null;
            return false;
        }

        public bool TryResolveAbility<T>(out T ability) where T : EntityAbility
        {
            if (m_RegisteredAbilities.TryGetValue(typeof(T), out var entityAbility))
            {
                ability = (T)entityAbility;
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

        protected virtual UniTask OnEntityRun(Ready ready, Signal signal)
        {
            return ready();
        }

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

        public object Save()
        {
            var entityTransform = transform;
            var componentsData = new EntityComponent.SaveData[Components.Count];
            var saveData = new SaveData(blueprint, id, isStatic, entityTransform.position, entityTransform.rotation)
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
            var entityData = (SaveData)data;
            var thisGameObject = gameObject;
            foreach (var componentSaveData in entityData.ComponentsData)
            {
                var component = thisGameObject.GetOrAddComponent<EntityComponent>(componentSaveData.ComponentType);
                component.Load(componentSaveData);
            }

            return UniTask.CompletedTask;
        }

        public UniTask Bootstrap()
        {
            var tasks = new List<UniTask>();
            foreach (var component in Components)
            {
                if (component is ISavable savableComponent)
                {
                    tasks.Add(savableComponent.Bootstrap());
                }
            }
            
            return UniTask.WhenAll(tasks);
        }
    }
}