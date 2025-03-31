using System;
using Cysharp.Threading.Tasks;
using Metamatic.Framework.Workshop;
using Metamatic.SaveSystem;
using UnityEngine;

namespace Metamatic.Framework
{
    /// <summary>
    /// Virtual (ghost) entities
    /// </summary>
    [Virtualization(typeof(Entity))]
    public class VirtualEntity: ISavable
    {
        public VirtualPlace place { get; internal set; }
        private bool m_Enabled = false;
        /// <summary>
        /// Determines the place where object is located.
        /// </summary>
        public string placeAddress;
        /// <summary>
        /// Object world position
        /// </summary>
        public Vector3 position;
        /// <summary>
        /// Entity id
        /// </summary>
        public string id;
        /// <summary>
        /// Entity blueprint
        /// </summary>
        public string blueprint;
        /// <summary>
        /// Is entity static (spawned within the scene). Static entities cannot be spawned or destroyed.
        /// </summary>
        public bool isStatic;
        /// <summary>
        /// Is entity removed from scene manually. It can be spawned again only by manual action.
        /// </summary>
        public bool isKicked;

        /// <summary>
        /// Id of the game area where entity was located when was materialized. It makes possible to spawn entity in the same game area when it's activated again.
        /// </summary>
        private Observable<string> m_ObservableGameAreaId = new();
        private Observable<string> m_ObservableCheckpointId = new();
        private SignalSource m_DisableSignalSource = default;
        /// <summary>
        /// Determines if entity is being materialized right now
        /// </summary>
        public bool isBeingMaterialized { get; private set; } = false;
        /// <summary>
        /// Determines that virtual entity is about to be destroyed
        /// </summary>
        public bool isAboutToDestroy;
        
        public string gameAreaId
        {
            get => m_ObservableGameAreaId.value;
            set => m_ObservableGameAreaId.value = value;
        }

        /// <summary>
        /// Optional checkpoint id. If specified, entity will be spawned related to this checkpoint instead of world position.
        /// E.g. if you translate entity to another place you can specify checkpoint 'default' and entity will be spawned at this checkpoint.
        /// </summary>
        public string checkpointId
        {
            get => m_ObservableCheckpointId.value;
            set => m_ObservableCheckpointId.value = value;
        }

        /// <summary>
        /// Helper property to fast get checkpoint object. E.g. if checkpointId is specified, you can get checkpoint object by this property.
        /// </summary>
        public Checkpoint checkpoint;
        
        public bool enabled
        {
            set
            {
                if (value == m_Enabled)
                {
                    return;
                }

                m_Enabled = value;
                if (value)
                {
                    m_DisableSignalSource.Dispose();
                    m_DisableSignalSource = SignalSource.Create();
                    OnEnableAsync(m_DisableSignalSource.Signal).Forget();
                }
                else
                {
                    m_DisableSignalSource.TryRaise();
                }
            }
            
            get => m_Enabled;
        }

        private bool m_HasData;
        private Entity.SaveData m_SaveData;
        
        public ObservableNullable<Entity> m_MaterializedEntity = new();

        public VirtualEntity(VirtualPlace place, string id, Vector3 position, string blueprintAddress = null)
        {
            this.place = place;
            this.id = id;
            blueprint = blueprintAddress;

            if (GameArea.TryGetIntersectedArea(position, true, out var gameArea))
            {
                m_ObservableGameAreaId.value = gameArea.id;
            }
        }
        
        internal void InternalAwake()
        {
            this.place.world.InternalRegisterVirtualEntity(this);
        }
        
        internal void InternalDestroy(DismissReason reason)
        {
            this.place.world.InternalUnregisterVirtualEntity(this);
        }

        internal void Release(DismissReason reason)
        {
            place.ReleaseEntity(this, reason);
        }
        
        public object Save()
        {
            return m_HasData
                ? new Entity.SaveData()
                {
                    id = id,
                    blueprint = blueprint,
                    type = m_SaveData.type,
                    isStatic = isStatic,
                    isKicked = isKicked,
                    position = position,
                    rotation = m_SaveData.rotation,
                    gameAreaId = m_ObservableGameAreaId.value,
                    checkpointId = checkpointId,
                    dataComponents = m_SaveData.dataComponents,
                    components = m_SaveData.components
                }
                : null;
        }

        public UniTask Load(object data)
        {
            if (data is Entity.SaveData saveData)
            {
                #if DEBUG
                if (id != saveData.id)
                {
                    Console.Error("Entity", "Load", $"Entity save data and virtual data mismatch - ids are different:{id} != {saveData.id}");
                }
                #endif
                id = saveData.id;
                m_SaveData = saveData;
                m_HasData = true;
                blueprint = saveData.blueprint;
                isStatic = saveData.isStatic;
                isKicked = saveData.isKicked;
                position = saveData.position;
                checkpointId = saveData.checkpointId;
                m_ObservableGameAreaId.uniqValue = saveData.gameAreaId;
                
                return UniTask.CompletedTask;
            }
            else
            {
                Console.Error("Entity", "Load", $"Unexpected data type: {data.GetType().Name}");
            }
            return UniTask.CompletedTask;
        }

        public void Hydrate(Entity entity)
        {
            var data = Save();
            if (data != null)
            {
                entity.Load(Save()); // TODO: Await load?
            }
            m_MaterializedEntity.value = entity;
        }

        public UniTask<Entity> WaitForMaterialization(Signal cancellationSignal)
        {
            return m_MaterializedEntity.WhenHasValue(cancellationSignal);
        }

        protected async UniTaskVoid OnEnableAsync(Signal cancelSignal)
        {
            {
                using var watchMaterialized = m_MaterializedEntity.WatchAsync(OnMaterializedChangeAsync);
                await cancelSignal;
            }
            TryDematerialize(isAboutToDestroy ? DismissReason.Release : DismissReason.Abandon);
        }
        
        public DisposableAction WatchMaterializedEntityChange(Action<Entity, bool> action)
        {
            return m_MaterializedEntity.Watch(action);
        }
        
        private async UniTaskVoid OnMaterializedChangeAsync(Entity entity, bool hasEntity, Signal cancellationSignal)
        {
            if (!hasEntity && !isKicked && !isStatic) // TODO: Watch kicked, because it can be changed anytime
            {
                if (String.IsNullOrEmpty(checkpointId))
                {
                    // Should be materialized in the specified game area
                    using var observeGameAreaId = m_ObservableGameAreaId.WatchAsync(OnGameAreaIdChangeAsync);
                    await cancellationSignal;
                }
                else
                {
                    using var observeCheckpointId = m_ObservableCheckpointId.WatchAsync(OnCheckpointIdChangeAsync);
                    await cancellationSignal;
                }
            }
        }

        private async UniTaskVoid OnGameAreaIdChangeAsync(string gameAreaId, Signal cancellationSignal)
        {
            if (String.IsNullOrEmpty(gameAreaId) || !GameArea.Exists(gameAreaId))
            {
                Console.Debug("VirtualEntity", "FindNewGameArea", id, $"Find new game area");
                // Find intersected game area
                if (GameArea.TryGetIntersectedArea(position, true, out var newArea))
                {
                    // Change virtual entity's game area
                    m_ObservableGameAreaId.value = newArea.id;
                }
                else
                {
                    Console.Error("VirtualEntity", "No intersected game area found");
                    // TODO: What to do if no intersected game area found?
                }
            }
            else
            {
                using var watchArea = GameArea.WatchGameAreaActivationAsync(gameAreaId, OnRelatedGameAreaActivated);
                await cancellationSignal;
            }
        }

        private async UniTaskVoid OnCheckpointIdChangeAsync(string checkpointId, Signal cancellationSignal)
        {
            if (checkpoint != null || Checkpoint.TryGetCheckpoint(checkpointId, out checkpoint))
            {
                using var watchCheckpointActivation = checkpoint.WatchActivationAsync(OnCheckpointActivationAsync);
                await cancellationSignal;
            }
            else
            {
                Console.Error("VirtualEntity", "OnCheckpointIdChangeAsync", $"Can not find checkpoint '{checkpointId}'");
                GameSystem.FatalError($"Checkpoint '{checkpointId}' not found");
            }
        }
        
        private async UniTaskVoid OnCheckpointActivationAsync(bool isActivated, Signal cancelSignal)
        {
            if (isActivated)
            {
                await UniTaskEventLoop.Shared.Yield(cancelSignal);
                var entity = await Entity.Spawn(blueprint, checkpoint.worldSpawnPosition,
                    Quaternion.identity, id, false, Save(), cancelSignal);
                entity.transform.position = checkpoint.worldSpawnPosition;
                entity.InternalBindToVirtualEntity(this);
                InternalBindToMaterializedEntity(entity);
                entity.gameObject.SetActive(true);
            }
        }

        private UniTaskVoid OnRelatedGameAreaActivated(GameArea area, bool isActive, Signal cancellationSignal)
        {
            Console.Debug("SpawnEntityFindProblem", "GameAreaActivated", $"Game area '{area.id}' is activated: {isActive}");
            if (isActive)
            {
                if (area.IsIntersected(position))
                {
                    Materialize(cancellationSignal); // TODO: Await it?
                }
                else
                {
                    // Oh, no! Entity is out of the game area.
                    // We need find another game area where entity can be materialized.
                    if (GameArea.TryGetIntersectedArea(position, true, out var newArea))
                    {
                        // Change virtual entity's game area
                        m_ObservableGameAreaId.value = newArea.id;
                    }
                    else
                    {
                        Console.Error("VirtualEntity", "No intersected game area found");
                        // TODO: What to do if no intersected game area found?
                    }
                }
            }

            return new UniTaskVoid();
        }

        public void Materialize(Signal cancellationSignal)
        {
            if (m_MaterializedEntity.hasValue)
            {
                Console.Error("VirtualEntity", "Materialize", $"Entity '{id}' is already materialized");
                GameSystem.FatalError(new Exception("Entity is already materialized"));
            } else if (isBeingMaterialized)
            {
                Console.Error("VirtualEntity", "Materialize", "Entity is being materialized");
                GameSystem.FatalError(new Exception($"Entity '{id}' is being materialized"));
            }
            InternalMaterializeAsync(cancellationSignal).Forget();
        }

        public void TryDematerialize(DismissReason reason)
        {
            if (m_MaterializedEntity.hasValue && !isStatic)
            {
                m_MaterializedEntity.value.Dematerialize(reason);
            }
        }
        
        /// <summary>
        /// Enable entity if it's VirtualPlace is not hibernated
        /// </summary>
        public void EnableIfNotHybernated()
        {
            if (!place.hibernated)
            {
                enabled = true;
            }
        }
        
        private async UniTaskVoid InternalMaterializeAsync(Signal cancellationSignal)
        {
            isBeingMaterialized = true;
            try
            {
                #if DEBUG
                if (blueprint == null)
                {
                    Console.Error("VirtualEntity", "InternalMaterializeAsync", $"Blueprint for entity {id} is not specified!");
                    return;
                }
                #endif
                var entity = await Entity.Spawn(blueprint, position,
                    Quaternion.identity, id, false, Save(), cancellationSignal);
                entity.InternalBindToVirtualEntity(this);
                InternalBindToMaterializedEntity(entity);
                entity.gameObject.SetActive(true);
            }
            catch (Exception e)
            {
                Console.Error("VirtualEntity", "InternalWaitForMaterializationAsync", e);
                GameSystem.FatalError(new Exception($"Entity {id} materialization failed"));
            }
            finally
            {
                isBeingMaterialized = false;
            }
        }
        
        public void InternalBindToMaterializedEntity(Entity entity)
        {
            #if DEBUG
            if (m_MaterializedEntity.hasValue)
            {
                Console.Error("VirtualEntity", "InternalBindToMaterializedEntity", "Entity is already bound");
                GameSystem.FatalError(new Exception("Entity is already bound"));
            }
            #endif
            m_MaterializedEntity.value = entity;
            entity.SubscribeDismissNoReasonOnce(m_MaterializedEntity.Clear);
        }
    }
}