using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using Metamatic.Exceptions;
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
    public sealed class Entity : AsyncBehaviour, IResettable
    {
        [Version("1.0.0")]
        public struct ComponentSaveData
        {
            public Type componentType;
            public object data;
        }
        
        [Serializable]
        [Version("1.0.0")]
        public struct SaveData
        {
            /// <summary>
            /// Address of the blueprint
            /// </summary>
            public string blueprint;
            // ReSharper disable once InconsistentNaming
            public Type type;
            public string id;
            public bool isStatic;
            public bool isKicked;
            public Vector3 position;
            public Quaternion rotation;
            public string gameAreaId;
            public string checkpointId;
            // public Dictionary<Type, EntityData.SaveData> dataset; // TODO: Implements dataset instead of dataComponents?
            public EntityData.SaveData[] dataComponents;
            public ComponentSaveData[] components;
            public EntityLogic.SaveData[] logics;
        }
        
        #region Structures

        private enum PostDematerializeBehaviour
        {
            KeepVirtualEntity,
            DestroyVirtualEntity,
            DestroyVirtualEntityWithDelay
        }
        #endregion
        
        #region Static API
        public static readonly SignedIndexedHashPool<Entity> EntitiesRegistry = new(200);
        public static readonly Dictionary<string, Token96Bit> EntitiesRegistrationTokens = new();
        public static readonly Dictionary<string, Action<Entity, bool>> EntitiesRegistrationEventListeners = new();
        
        private static event Action<Entity> OnEntityRegisteredEvent;
        private static event Action<Entity, DismissReason> OnEntityUnregisteredEvent;
        
        private readonly ObservableNullable<GameArea> m_ObservableGameArea = new ObservableNullable<GameArea>();

        /// <summary>
        /// Contains the observable game area where the entity is located
        /// </summary>
        public GameArea gameArea
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_ObservableGameArea.Value;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private set => m_ObservableGameArea.Value = value;
        }
        
        /// <summary>
        /// Determines that the game area is set
        /// </summary>
        private bool gameAreaIsSet
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_ObservableGameArea.hasValue;
        }
        
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
            
            if (TryGetEntityById(id, out var entity))
            {
                listener(entity, true);
            }
            
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
        public static async UniTask<Entity> Spawn(
            string blueprint,
            Vector3 position = default,
            Quaternion rotation = default,
            string id = null,
            bool activate = false,
            object saveData = default,
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
                    summon = GameObjectFactory.SummonFromBlueprint<Entity>(blueprint);
                    entity = await summon.Wait(cancellationSignal);
                    var entityTransform = entity.transform;
                    
                    entityTransform.position = position;
                    entityTransform.rotation = rotation;

                    entity.Initialize(blueprint, id);

                    cancellationSignal.ThrowIfRaised();
                    // Release blueprint and prefab after entity is destroyed
                    entity.OnEntityDismissedEvent += (reason) =>
                    {
                        // We can dispose blueprint and prefab only after entity is destroyed
                        summon.Dispose();
                    };
                    // Load data
                    await entity.Materialize(saveData);
                }
                catch (OperationCanceledException)
                {
                    summon.Dispose();
                    throw;
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
        
        [Tooltip("Always play virtual logic even if entity is materialized (Experimental)")]
        public bool alwaysPlayVirtualLogic = false;
        
        [Tooltip("Destroy virtual entity when entity is dematerialized")]
        public bool destroyVirtualEntityOnDematerialize = true; // TODO: Implement destroyVirtualEntityOnDematerialize logic
        
        [ShowIf("destroyVirtualEntityOnDematerialize")]
        [Tooltip("Delay before destroy virtual entity")]
        public float destroyVirtualEntityOnDematerializeDelay = 0f; // TODO: Implement destroyVirtualEntityDelay logic

        #endregion
        
        #region Public API
        public string blueprint { get; private set; }
        public readonly ObservableList<EntityData> DataComponents = new ObservableList<EntityData>();
        /// <summary>
        /// Determines that entity was not spawned dynamically
        /// </summary>
        public bool isStatic { get; private set; } = true;
        /// <summary>
        /// The entity is kicked from the world. It means that it should be virtualized and stay in the virtual world until it will be revived.
        /// </summary>
        public bool isKicked { get; private set; }
        public VirtualEntity virtualEntity { get; private set; }
        /// <summary>
        /// The event should be raised when entity data is loaded
        /// </summary>
        public event Action OnMaterialized;
        /// <summary>
        /// The event should be raised before entity saves its data when it is going to be dematerialized 
        /// </summary>
        public event Action OnBeforeDematerialize;
        public bool loaded { get; private set; }

        /// <summary>
        /// Entity is marked as virtualized. It means that it is not in the world and can be returned back to the world.
        /// Even if entity is still in the world, this flag indicates that it already virtualized and can be safely destroyed.
        /// </summary>
        public DismissReason isDismissed { get; private set; }= DismissReason.None;
        public event Action<DismissReason> OnEntityDismissedEvent;

        /// <summary>
        /// Watch entity lifecycle. Signal will be raised when entity is dismissed.
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public DisposableAction WatchEntityLifecycle(Func<Entity, UniTask<DismissReason>, UniTaskVoid> handler)
        {
            var dismissedTaskCs = SafeUniTaskCompletionSource<DismissReason>.Create();
            OnEntityDismissedEvent += (reason) =>
            {
                dismissedTaskCs.TrySetResult(reason);
            };
            handler(this, dismissedTaskCs.Task).Forget();
            return new DisposableAction(() =>
            {
                dismissedTaskCs.TrySetCanceled();
            });
        }

        /// <summary>
        /// Watch entity materialization lifecycle. Signal will be raised when entity is dematerialized.
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public DisposableAction WatchMaterializedAsync(Func<Signal, UniTaskVoid> handler)
        {
            SignalSource css = default;
            if (loaded)
            {
                css = SignalSource.Create();
                handler(css.Signal).Forget();
            }

            OnMaterialized += MaterializedHandler;
            OnBeforeDematerialize += DematerializedHandler;
            
            return new DisposableAction(() =>
            {
                OnMaterialized -= MaterializedHandler;
                OnBeforeDematerialize -= DematerializedHandler;
                css.TryRaise();
            });

            void MaterializedHandler()
            {
                css.Dispose();
                css = SignalSource.Create();
                handler(css.Signal).Forget();
            }
            
            void DematerializedHandler()
            {
                css.TryRaise();
            }
        }
        
        public DisposableAction SubscribeDismissOnce(Action<DismissReason> handler)
        {
            if (isDismissed != DismissReason.None)
            {
                handler(isDismissed);
                return DisposableAction.Empty;
            }
            void Handler(DismissReason reason)
            {
                handler(reason);
                OnEntityDismissedEvent -= Handler;
            }
            OnEntityDismissedEvent += Handler;
            return new DisposableAction(() => OnEntityDismissedEvent -= Handler);
        }
        
        public DisposableAction SubscribeDismissNoReasonOnce(Action handler)
        {
            if (isDismissed != DismissReason.None)
            {
                handler();
                return DisposableAction.Empty;
            }
            void Handler(DismissReason reason)
            {
                handler();
                OnEntityDismissedEvent -= Handler;
            }
            OnEntityDismissedEvent += Handler;
            return new DisposableAction(() => OnEntityDismissedEvent -= Handler);
        }
        
        public UniTask<DismissReason> WaitForDismissal(Signal cancellationSignal) {
            if (isDismissed != DismissReason.None)
            {
                return UniTask.FromResult(isDismissed);
            }
            var completionSource = SafeUniTaskCompletionSource<DismissReason>.Create();
            DisposableAction cancellationListener = default;
            OnEntityDismissedEvent += (reason) =>
            {
                cancellationListener.Dispose();
                completionSource.TrySetResult(reason);
            };
            cancellationListener = cancellationSignal.Register(() =>
            {
                completionSource.TrySetCanceled();
            });
            return completionSource.Task;
        }
        
        /// <summary>
        /// Dismiss entity with the specified reason. Entities created by the pool should be returned to the pool.
        /// Static entities can not be dismissed.
        /// </summary>
        /// <param name="reason"></param>
        private void Dismiss(DismissReason reason)
        {
            if (isDismissed != DismissReason.None)
            {
                return;
            }

            isDismissed = reason;
            TryUnregisterEntity(m_RegistrationToken, reason);
            m_ObservableGameArea.Clear();
            /* Clear its game area */
            OnEntityDismissedEvent?.Invoke(reason);
            OnEntityDismissedEvent = null;
            if (!isStatic)
            {
                /* Remove its id when recycled */
                id = null;
            }
        }

        /// <summary>
        /// Released entity won't be virtualized and will be dismissed with the reason Destroy.
        /// Use it if you want to destroy the entity.
        /// </summary>
        public void Release()
        {
            Dematerialize(DismissReason.Release);
        }
        
        /// <summary>
        /// Killed entity won't be virtualized and will be dismissed with the reason Kill
        /// </summary>
        /// <param name="reason"></param>
        public void Kill(KillReason reason)
        {
            GameSystem.virtualWorld.ReleaseEntity(id, DismissReason.Kill);
        }
        
        /// <summary>
        /// Manual move entity to virtual world. It can be returned back to the world by explicit request.
        /// </summary>
        public void Kick()
        {
            isKicked = true;
            Dematerialize(DismissReason.Kick);
        }
        
        /// <summary>
        /// Virtualize and dismiss entity
        /// </summary>
        public void Virtualize()
        {
            Console.Debug("Entity", "Virtualize", $"Virtualizing entity {id} in virtual world: ${GameSystem.virtualWorld}");;
            GameSystem.virtualWorld.Virtualize(this);
        }

        public void Dematerialize(DismissReason reason)
        {
            if (isDismissed != DismissReason.None)
            {
                return;
            }
            Console.Debug("Entity", "Dematerialize", $"Dematerializing entity {id}");
            OnBeforeDematerialize?.Invoke();
            loaded = false;
            var thisId = id;
            
            PostDematerializeBehaviour postDematerializeBehaviour = PostDematerializeBehaviour.KeepVirtualEntity;
            if (
                reason == DismissReason.Kill
                || reason == DismissReason.Release
            )
            {
                postDematerializeBehaviour = PostDematerializeBehaviour.DestroyVirtualEntity;
            }
            else if (reason == DismissReason.Kick)
            {
                postDematerializeBehaviour = PostDematerializeBehaviour.KeepVirtualEntity;
            }
            else if (destroyVirtualEntityOnDematerialize)
            {
                if (destroyVirtualEntityOnDematerializeDelay > 0)
                {
                    postDematerializeBehaviour = PostDematerializeBehaviour.DestroyVirtualEntityWithDelay;
                }
                else
                {
                    postDematerializeBehaviour = PostDematerializeBehaviour.DestroyVirtualEntity;
                }
            }
            
            switch (postDematerializeBehaviour)
            {
                case PostDematerializeBehaviour.KeepVirtualEntity:
                    case PostDematerializeBehaviour.DestroyVirtualEntityWithDelay:
                    
                        Virtualize();
                        break;
            }

            if (postDematerializeBehaviour == PostDematerializeBehaviour.DestroyVirtualEntityWithDelay)
            {
                
            }
            
            Dismiss(reason);

            /*
             * After dismissing entity, we can destroy virtual entity if needed
             * It is important to destroy virtual entity after dismissing, because
             * releasing entity will invoke dematerialize method again if entity is not dismissed
             */
            switch (postDematerializeBehaviour)
            {
                case PostDematerializeBehaviour.DestroyVirtualEntity:
                    VirtualWorld.Current?.TryReleaseVirtualEntityInCurrentPlaceIfNotMaterialized(thisId, reason);
                    break;
                case PostDematerializeBehaviour.DestroyVirtualEntityWithDelay:
                    TimeService.Schedule(destroyVirtualEntityOnDematerializeDelay, () =>
                    {
                        VirtualWorld.Current?.TryReleaseVirtualEntityInCurrentPlaceIfNotMaterialized(thisId, reason);
                    }, PlayerLoopTiming.PostLateUpdate);
                    break;
            }
        }

        /// <summary>
        /// This method is called when the entity is out of any active game area
        /// </summary>
        private void OnOutOfGameArea()
        {
            /* Search for active game area */
            if (GameArea.TryGetIntersectedArea(transform.position, true, out var area))
            {
                Console.Debug("Debug1", "SetGameArea", $"Set game area {area} for entity {id}");
                gameArea = area;
                if (!area.isActive)
                {
                    if (isStatic)
                    {
                        var thisTransform = transform;
                        thisTransform.position = m_InitialPosition;
                        thisTransform.rotation = m_InitialRotation;
                    }
                    else
                    {
                        Dematerialize(DismissReason.Abandon);
                    }
                }
            }
            else
            {
                if (isStatic)
                {
                    // Static objects can't be killed
                    // So it should be returned to its initial position
                    // Restore initial position and rotation
                    var thisTransform = transform;
                    thisTransform.position = m_InitialPosition;
                    thisTransform.rotation = m_InitialRotation;
                }
                else
                {
                    // Kill, because object is out of any areas
                    // TODO: Are you sure that you want to kill the entity?
                    Kill(KillReason.LeaveGameArea);
                }
            }
        }

        private async UniTaskVoid OnGameAreaChange(GameArea gameArea, bool hasValue, Signal signal)
        {
            if (!hasValue)
            {
                return;
            }
            using var entityRegistration = gameArea.Register(this);
            await signal;
        }
        
        public void RegisterComponent(EntityData component)
        {
            if (!m_RegisteredComponents.ContainsKey(component.GetType()))
            {
                m_RegisteredComponents.Add(component.GetType(), component);
                DataComponents.Add(component);
            }
        }

        public void UnregisterComponent(EntityData component)
        {
            if (!m_RegisteredComponents.ContainsKey(component.GetType()))
            {
                throw new InvalidOperationException($"Component of type {component.GetType().Name} is not registered");
            }

            DataComponents.Remove(component);
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
        /// TODO: Do we need this method?
        /// </summary>
        /// <param name="nextBlueprint"></param>
        /// <param name="nextId"></param>
        /// <returns></returns>
        public string Initialize(string address, string nextId)
        {
            this.id = nextId;
            this.blueprint = address;
            this.isStatic = false;
            return this.id;
        }

        public T ResolveComponent<T>() where T : EntityData
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

        public bool TryResolveComponent<T>(out T component) where T : EntityData
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

        /// <summary>
        /// This method is called before the entity is returned to the pool (via IResettable interface. See. Reset())
        /// </summary>
        public void ResetEntity()
        {
            /* Erase id and game area */
            id = null;
            isKicked = false;
            m_ObservableGameArea.Clear();
            isDismissed = DismissReason.None;
            virtualEntity = null;
            
            OnEntityResetEvent?.Invoke();
        }
        
        #endregion
        
        #region Private API
        private Token96Bit m_RegistrationToken;
        private void GenerateUniqueId()
        {
            id = IdsGenerator.GenerateGuid();
        }
        private event Action OnEntityResetEvent;
        private event Action OnEntityUpdateEvent;
        private readonly Dictionary<Type, EntityData> m_RegisteredComponents =
            new Dictionary<Type, EntityData>();

        private readonly Dictionary<Type, EntityAbility> m_RegisteredAbilities = new Dictionary<Type, EntityAbility>();
        private Vector3 m_InitialPosition;
        private Quaternion m_InitialRotation;
        public event Action OnHydrated;
        #endregion
        
        /// <summary>
        /// Watch entity hydrated event (when its virtual entity is synchronized with it). Signal will be raised when entity is dehydrated (not implemented yet) or watcher is disposed.
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public DisposableAction WatchHydratedAsync(Func<Signal, UniTaskVoid> handler)
        {
            SignalSource css = default;
            if (virtualEntity != null)
            {
                css = SignalSource.Create();
                handler(css.Signal).Forget();
                return DisposableAction.Create(css.TryRaise);
            }
            void Handler()
            {
                css.TryRaise();
                css = SignalSource.Create();
                handler(css.Signal).Forget();
                OnHydrated -= Handler;
            }
            OnHydrated += Handler;
            return new DisposableAction(() =>
            {
                OnHydrated -= Handler;
                css.TryRaise();
            });
        }
        
        #region Mono
        
        protected override void Awake()
        {
            m_InitialPosition = transform.position;
            m_InitialRotation = transform.rotation;
            base.Awake();

#if UNITY_EDITOR
            if (EditorUtility.IsPersistent(gameObject))
            {
                Debug.Log("Это префаб-ассет (не инстанцированный объект в сцене)");
            }
#endif
        }

        private void FixedUpdate()
        {
            if (isKicked) return;
            /* First of all check if entity is still inside active game area */
            var position = transform.position;
            if (!gameAreaIsSet || !gameArea.isActive || !gameArea.IsIntersected(position))
            {
                m_ObservableGameArea.Clear();
                /* Search for the game area */
                foreach (var area in GameArea.ActiveAreas)
                {
                    if (area.IsIntersected(position))
                    {
                        Console.Debug("Debug1", "SetGameArea", $"Set game area {area} for entity {id}");
                        gameArea = area;
                        break;
                    }
                }

                if (!gameAreaIsSet)
                {
                    OnOutOfGameArea();
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

            if (virtualEntity == null)
            {
                InternalTryHydrateVirtualEntity().Forget();
            }
        }
        
        private async UniTaskVoid InternalTryHydrateVirtualEntity()
        {
            try
            {
                await GameSystem.virtualWorld.TryHydrate(this);
                OnHydrated?.Invoke();
            } catch (Exception e)
            {
                Console.Error("Entity", "InternalTryHydrateVirtualEntity", $"Error while hydrating virtual entity: {e}");
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            // TODO: Here need to add some protected from destroying the entity without its virtualization. Check to kill. Reason of destruction.

            if (loaded)
            {
                // Force call dematerialize if it's not dematerialized yet to stop every subscription
                Console.Debug("Entity", "OnDestroy", $"Force dematerializing entity {id}");
                loaded = false;
                OnBeforeDematerialize?.Invoke();
            }
            Dismiss(DismissReason.Destroy);
            m_ObservableGameArea.Dispose();
        }
        #endregion

        protected override async UniTask Run(Ready ready, Signal signal)
        {
            m_RegistrationToken = TryRegisterEntity(this);
            using var watchGameArea = m_ObservableGameArea.WatchAsync(OnGameAreaChange);
            await ready();
        }

        public object Save()
        {
            var entityTransform = transform;
            var dataComponents = new EntityData.SaveData[DataComponents.Count];
            
            // Get all game object's components which implements ISavable interface
            var components = gameObject.GetComponents<MonoBehaviour>();
            var savableComponents = new List<IAutoSavable>();
            var logics = new List<EntityLogic.SaveData>();
            foreach (var component in components)
            {
                if (component is EntityLogic entityLogic)
                {
                    var logicSaveData = entityLogic.InternalSaveEntityLogic();
                    logics.Add(logicSaveData);
                } else if (component is IAutoSavable savable and not EntityData)
                {
                    savableComponents.Add(savable);
                }
            }
            
            var otherComponents = new ComponentSaveData[savableComponents.Count];
            
            var gameAreaId = gameAreaIsSet ? gameArea.id : null;
            var saveData = new SaveData()
            {
                blueprint = blueprint,
                type = this.GetType(),
                id = id,
                isStatic = isStatic,
                isKicked = isKicked,
                position =  entityTransform.position,
                rotation = entityTransform.rotation,
                gameAreaId = gameAreaId,
                checkpointId = null,
                dataComponents = dataComponents,
                components = otherComponents,
                logics = logics.ToArray()
            };
            var j = 0;
            foreach (var component in DataComponents)
            {
                dataComponents[j] = (EntityData.SaveData)component.Save();
                j++;
            }
            
            j = 0;
            foreach(var savableComponent in savableComponents)
            {
                otherComponents[j] = new ComponentSaveData()
                {
                    componentType = savableComponent.GetType(),
                    data = savableComponent.Save()
                };
                j++;
            }

            return saveData;
        }

        /// <summary>
        /// Save data within materializing
        /// </summary>
        /// <param name="data"></param>
        public async UniTask Materialize(object data)
        {
#if DEBUG
            if (loaded)
            {
                Console.Error("Entity", "Cannot materialize entity", $"Entity {id} is already materialized");
            }
#endif
            await LoadFromWorld(data);
            OnMaterialized?.Invoke();
            loaded = true;
        }

        /// <summary>
        /// This method is called when entity (probably static) has no virtual entity so it should be considered as materialized because there is no virtual data for it.
        /// Ensure that you call this method only after game place is loaded to be sure that entity is not virtualized for real.
        /// </summary>
        public void InternalMarkMaterialized()
        {
            #if DEBUG
            if (loaded)
            {
                Console.Error("Entity", "Cannot mark entity as materialized", $"Entity {id} is already materialized");
            }
            #endif
            OnMaterialized?.Invoke();
            loaded = true;
        }

        public async UniTask LoadFromWorld(object data)
        {
            if (data == null)
            {
                return;
            }
            var entityData = (SaveData)data;
            var thisGameObject = gameObject;
            var thisTransform = transform;

            blueprint = entityData.blueprint;
            id = entityData.id;
            isStatic = entityData.isStatic;
            isKicked = false;
            if (!String.IsNullOrEmpty(entityData.gameAreaId))
            {
                if (GameArea.TryGetAreaById(entityData.gameAreaId, out var resolvedGameArea))
                {
                    Console.Debug("Debug1", "SetGameArea", $"Set game area {resolvedGameArea} for entity {id}");
                    gameArea = resolvedGameArea;
                }
            }
            thisTransform.position = entityData.position;
            thisTransform.rotation = entityData.rotation;

            var loadingTasks = ParamLessPool<List<UniTask>>.GetShared();
            try
            {
                foreach (var componentSaveData in entityData.dataComponents)
                {
                    var component = thisGameObject.GetOrAddComponent<EntityData>(componentSaveData.ComponentType);
                    loadingTasks.Add(component.Load(componentSaveData));
                }

                foreach (var componentData in entityData.components)
                {
                    var component = thisGameObject.GetOrAddComponent<MonoBehaviour>(componentData.componentType);
                    if (component is IAutoSavable savable)
                    {
                        loadingTasks.Add(savable.Load(componentData.data));
                    }
                }
            
                foreach (var logicData in entityData.logics)
                {
                    var component = thisGameObject.GetOrAddComponent<EntityLogic>(logicData.componentType);
                    loadingTasks.Add(component.InternalLoadEntityLogic(logicData));
                }
                await UniTask.WhenAll(loadingTasks);
            }
            catch (Exception e)
            {
                Console.Debug("Entity", "Load", $"Error while loading entity {id}: {e}");
            }
            finally
            {
                loadingTasks.Clear();
                ParamLessPool<List<UniTask>>.ReturnShared(loadingTasks);
            }
        }

        public void InternalBindToVirtualEntity(VirtualEntity virtualEntity)
        {
            #if DEBUG
            if (this.virtualEntity != null)
            {
                GameSystem.FatalError($"Entity {id} is already bound to virtual entity");
            }
            #endif
            this.virtualEntity = virtualEntity;
            OnHydrated?.Invoke();
        }

        public void Reset()
        {
            if (Application.isPlaying)
            {
                ResetEntity();
            }
        }
    }
}