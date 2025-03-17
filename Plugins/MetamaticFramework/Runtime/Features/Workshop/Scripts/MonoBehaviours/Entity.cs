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
    [Virtualization(typeof(Entity))]
    public class VirtualEntity : VirtualBehaviour, ISavable
    {
        public string id { get; private set; }
        private object m_SaveData;
        
        /// <summary>
        /// Reference to original entity if it exists
        /// </summary>
        public Entity origin { get; private set; }

        public VirtualEntity(string id)
        {
            this.id = id;
        }
        
        public object Save()
        {
            if (origin == null)
            {
                return m_SaveData;
            }
            if (origin is ISavable savable)
            {
                return savable.Save();
            }
            return null;
        }

        public UniTask Load(object data)
        {
            if (origin == null)
            {
                m_SaveData = data;
                return UniTask.CompletedTask;
            }
            if (origin is ISavable savable)
            {
                return savable.Load(data);
            }
            return UniTask.CompletedTask;
        }
    }
    
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
            public Type type;
            public string id;
            public bool isStatic;
            public bool isKicked;
            public KickReason kickReason;
            public Vector3 position;
            public Quaternion rotation;
            public string gameAreaId;
            public EntityComponent.SaveData[] ComponentsData;
        }
        
        #region Static API
        public static readonly SignedIndexedPool<Entity> EntitiesRegistry = new(200);
        public static readonly Dictionary<string, Token96Bit> EntitiesRegistrationTokens = new();
        public static readonly Dictionary<string, Action<Entity, bool>> EntitiesRegistrationEventListeners = new();
        
        private static event Action<Entity> OnEntityRegisteredEvent;
        private static event Action<Entity, DismissReason> OnEntityUnregisteredEvent;
        
        /// <summary>
        /// Contains the observable game area where the entity is located
        /// </summary>
        public GameArea gameArea { get; private set; }
        /// <summary>
        /// Determines that the game area is set
        /// </summary>
        private bool m_GameAreaIsSet;
        
        [ShowInInspector]
        public static int entitiesCount => EntitiesRegistry.Count;
        
        /// <summary>
        /// Try to register entity in the registry. If entity already registered, returns empty token.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static Token96Bit TryRegisterEntity(Entity entity)
        {
            if (EntitiesRegistrationTokens.ContainsKey(entity.id))
            {
                return Token96Bit.Empty;
            }
            var registrationToken = EntitiesRegistry.Add(entity);
            var result = EntitiesRegistrationTokens[entity.id] = registrationToken;
            if (EntitiesRegistrationEventListeners.TryGetValue(entity.id, out var listener))
            {
                listener(entity, true);
            }
            OnEntityRegisteredEvent?.Invoke(entity);
            return result;
        }
        
        /// <summary>
        /// Try to unregister entity from the registry. If entity not found, returns false.
        /// </summary>
        /// <returns></returns>
        public static bool TryUnregisterEntity(Token96Bit registrationToken, DismissReason reason)
        {
            if (EntitiesRegistry.Remove(registrationToken, out var entity))
            {
                EntitiesRegistrationTokens.Remove(entity.id);
                if (!EntitiesRegistrationEventListeners.TryGetValue(entity.id, out var listener)) return true;
                EntitiesRegistrationEventListeners.Remove(entity.id);
                listener(entity, false);
                OnEntityUnregisteredEvent?.Invoke(entity, reason);
                return true;
            }

            return false;
        }
        
        public static bool TryGetEntityById(string id , out Entity result)
        {
            if (EntitiesRegistrationTokens.TryGetValue(id, out var token))
            {
                if (EntitiesRegistry.TryGet(token, out var entity))
                {
                    result = entity;
                    return true;
                }
            }

            result = null;
            return false;
        }
        
        public static DisposableAction WatchEntity(string id, Action<Entity, bool> listener)
        {
            if (EntitiesRegistrationEventListeners.TryGetValue(id, out var existingListener))
            {
                EntitiesRegistrationEventListeners[id] = existingListener + listener;
            }
            else
            {
                EntitiesRegistrationEventListeners[id] = listener;
            }
            EntitiesRegistrationEventListeners[id] = listener;
            return new DisposableAction(() =>
            {
                if (EntitiesRegistrationEventListeners.TryGetValue(id, out var postExistingListener))
                {
                    postExistingListener -= listener;
                    if (postExistingListener == null)
                    {
                        EntitiesRegistrationEventListeners.Remove(id);
                    }
                    else
                    {
                        EntitiesRegistrationEventListeners[id] = postExistingListener;
                    }
                }
            });
        }

        public static DisposableAction WatchEntityAsync(string id, Func<Entity, Signal, UniTaskVoid> handler)
        {
            SignalSource cancellationSignalSource = SignalSource.Create();
            var cancellationSignal = cancellationSignalSource.Signal;
            SignalSource releaseSignalSource = default;
            var listener = WatchEntity(id, (entity, registered) =>
            {
                if (registered)
                {
                    releaseSignalSource = SignalSource.Create();
                    var signal = releaseSignalSource.Signal;
                    handler(entity, signal).Forget();
                }
                else
                {
                    releaseSignalSource.TryRaise();
                }
            });
            return new DisposableAction(() =>
            {
                listener.Dispose();
                releaseSignalSource.TryRaise();
                cancellationSignalSource.TryRaise();
            });
        }
        
        public static UniTask<Entity> WaitForEntity(string id, Signal cancellationSignal)
        {
            if (EntitiesRegistrationTokens.TryGetValue(id, out var token))
            {
                if (EntitiesRegistry.TryGet(token, out var result))
                {
                    return UniTask.FromResult(result);
                }
            }
            var completionSource = SafeUniTaskCompletionSource<Entity>.Create();
            DisposableAction signalListener = default;
            var listener = WatchEntity(id, (entity, registered) =>
            {
                if (registered)
                {
                    signalListener.Dispose();
                    completionSource.TrySetResult(entity);
                }
                else
                {
                    Console.Error("EntitiesService", $"Unexpected behaviour. Entity with id {id} was unregistered before it was registered");
                }
            });
            signalListener = cancellationSignal.Register(() =>
            {
                completionSource.TrySetCanceled();
                listener.Dispose();
            });
            return completionSource.Task;
        }
        
        /// <summary>
        /// Spawn entity by blueprint reference. You can provide an id, in this case the entity will be reused if it already exists on the scene (in this case you alike to say 'I need this entity to be here - on scene').
        /// </summary>
        /// <param name="blueprintRef"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="id"></param>
        /// <param name="activate"></param>
        /// <param name="cancellationSignal"></param>
        /// <returns></returns>
        public static async UniTask<Entity> SpawnEntity(
            AssetReferenceT<EntityBlueprint> blueprintRef,
            Vector3 position = default,
            Quaternion rotation = default,
            string id = null,
            bool activate = false,
            Signal cancellationSignal = default
        )
        {
            Entity entity;

            if (id != null && Entity.TryGetEntityById(id, out entity))
            {
                // Set entity position and rotation
                var entityTransform = entity.transform;
                entityTransform.position = position;
                entityTransform.rotation = rotation;
            }
            else
            {
                AsyncSummon<Entity> summon = default;
                try
                {
                    // TODO: Here we can check if the pool for blueprint exists and if it does, we can spawn from the pool
                    
                    /* Summon blueprint */
                    summon = GameObjectFactory.SummonFromBlueprint<Entity>((AssetReference)blueprintRef);
                    entity = await summon.Wait(cancellationSignal);
                    var entityTransform = entity.transform;
                    
                    entityTransform.position = position;
                    entityTransform.rotation = rotation;

                    cancellationSignal.ThrowIfRaised();
                    // Release blueprint and prefab after entity is destroyed
                    entity.OnEntityDismissedEvent += (DismissReason reason) =>
                    {
                        if (reason != DismissReason.Destroy)
                        // We can dispose blueprint and prefab only after entity is destroyed
                        summon.Dispose();
                    };
                    // Add entity to the dictionary
                    
                }
                catch (OperationCanceledException e)
                {
                    summon.Dispose();
                    return null;
                }
            }

            if (activate)
            {
                entity.gameObject.SetActive(true);
            }

            return entity;
        }
        #endregion
        

        #region Serializable fields

        [Tooltip("Unique identifier of the entity")]
        [ReadOnly] public string id;

        #endregion
        
        #region Public API
        public AssetReferenceT<EntityBlueprint> blueprint { get; private set; }
        public readonly ObservableList<EntityComponent> Components = new ObservableList<EntityComponent>();
        /// <summary>
        /// Determines that entity was not spawned dynamically
        /// </summary>
        public bool isStatic { get; private set; } = true;
        /// <summary>
        /// The entity is kicked from the world.
        /// </summary>
        public bool isKicked { get; private set; } = false;
        public KickReason kickReason { get; private set; } = KickReason.None;
        public event Action<DismissReason> OnEntityDismissedEvent;
        
        /// <summary>
        /// Dismiss entity with the specified reason. Entities created by the pool should be returned to the pool.
        /// Static entities can not be dismissed.
        /// </summary>
        /// <param name="reason"></param>
        public void Dismiss(DismissReason reason)
        {
            if (isStatic)
            {
                throw new InvalidOperationException("Static entities can not be dismissed");
            }
            OnEntityDismissedEvent?.Invoke(reason);
            OnEntityDismissedEvent = null;
        }
        
        public void Kill(KillReason reason)
        {
            Dismiss(DismissReason.Kill);
        }
        
        public void Kick(KickReason reason)
        {
            isKicked = true;
            kickReason = reason;
            /* Resolve game area */
            
            GameSystem.virtualWorld.VirtualizeEntity(this);
            Dismiss(DismissReason.Kick);
        }

        /// <summary>
        /// This method is called when the entity is out of any active game area
        /// </summary>
        private void OutOfArea()
        {
            /* Search for inactive game area */
            if (GameArea.TryFindIntersectArea(transform.position, true, out var area))
            {
                gameArea = area;
                m_GameAreaIsSet = true;
                Kick(KickReason.Abandon);
            }
            else
            {
                if (isStatic)
                {
                    // Static objects can't be killed
                    // So it should be returned to its initial position
                    // TODO: Return static entity to its initial position
                }
                else
                {
                    // Kill, because object is out of any areas
                    // TODO: Are you sure that you want to kill the entity?
                    Kill(KillReason.Exception);
                }
            }
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
        
        /// <summary>
        /// This method must be called to initialize the entity when it has been created from prefab
        /// </summary>
        /// <param name="nextBlueprint"></param>
        /// <param name="nextId"></param>
        /// <param name="isStatic"></param>
        /// <returns></returns>
        public string Initialize(AssetReferenceT<EntityBlueprint> nextBlueprint, string nextId)
        {
            this.id = nextId;
            this.blueprint = nextBlueprint;
            this.isStatic = false;
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
        
        #endregion
        
        private Token96Bit m_RegistrationToken;
        private void GenerateUniqueId(bool force = false)
        {
            id = Guid.NewGuid().ToString();
        }
        private event Action OnEntityResetEvent;
        private event Action OnEntityUpdateEvent;
        private readonly Dictionary<Type, EntityComponent> m_RegisteredComponents =
            new Dictionary<Type, EntityComponent>();

        private readonly Dictionary<Type, EntityAbility> m_RegisteredAbilities = new Dictionary<Type, EntityAbility>();

        

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
            if (gameObject.TryGetComponent<RecyclableBehaviour>(out var recyclable))
            {
                recyclable.OnRecycle += (reason) =>
                {
                    /* Remove its id when recycled */
                    id = null;
                };
            }
        }

        // TODO: Update entity according to its LOD level
        protected virtual void Update()
        {
            OnEntityUpdateEvent?.Invoke();
        }

        private void FixedUpdate()
        {
            if (isKicked) return;
            /* First of all check if entity is still inside active game area */
            var position = transform.position;
            if (!m_GameAreaIsSet || !gameArea.IsIntersected(position))
            {
                m_GameAreaIsSet = false;
                /* Search for the game area */
                foreach (var area in GameArea.ActiveAreas)
                {
                    if (area.IsIntersected(position))
                    {
                        gameArea = area;
                        m_GameAreaIsSet = true;
                        break;
                    }
                }

                if (!m_GameAreaIsSet)
                {
                    OutOfArea();
                }
            }
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

        
        protected void Start()
        {
            if (String.IsNullOrEmpty(id))
            {
                /* Entity become automatically non static if it has no id at start */
                isStatic = false;
                id = Guid.NewGuid().ToString();
            }
            m_RegistrationToken = Entity.TryRegisterEntity(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Entity.TryUnregisterEntity(m_RegistrationToken, DismissReason.Destroy);
        }
        #endregion

        protected override async UniTask Run(Ready ready, Signal signal)
        {
            using var announce = Dispatcher.global.Announce(this, false);
            await OnEntityRun(ready, signal);
        }

        protected virtual UniTask OnEntityRun(Ready ready, Signal signal)
        {
            return ready();
        }

        protected virtual void OnEntityReset()
        {
            /* Override this method to implement custom reset logic */
        }

        public object Save()
        {
            var entityTransform = transform;
            var componentsData = new EntityComponent.SaveData[Components.Count];
            var gameAreaId = m_GameAreaIsSet ? gameArea.id : null;
            var saveData = new SaveData()
            {
                blueprint = blueprint,
                type = this.GetType(),
                id = id,
                isStatic = isStatic,
                isKicked = isKicked,
                kickReason = kickReason,
                position =  entityTransform.position,
                rotation = entityTransform.rotation,
                gameAreaId = gameAreaId,
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
            var thisTransform = transform;

            blueprint = entityData.blueprint;
            id = entityData.id;
            isStatic = entityData.isStatic;
            isKicked = false;
            kickReason = KickReason.None;
            if (!String.IsNullOrEmpty(entityData.gameAreaId))
            {
                if (GameArea.TryGetAreaById(entityData.gameAreaId, out var resolvedGameArea))
                {
                    m_GameAreaIsSet = true;
                    this.gameArea = resolvedGameArea;
                }
            }
            thisTransform.position = entityData.position;
            thisTransform.rotation = entityData.rotation;
            
            foreach (var componentSaveData in entityData.ComponentsData)
            {
                var component = thisGameObject.GetOrAddComponent<EntityComponent>(componentSaveData.ComponentType);
                component.Load(componentSaveData);
            }

            return UniTask.CompletedTask;
        }
    }
}