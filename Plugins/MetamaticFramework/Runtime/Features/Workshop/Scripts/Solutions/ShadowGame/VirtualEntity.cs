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
        /// Optional checkpoint id. If specified, entity will be spawned related to this checkpoint instead of world position.
        /// E.g. if you translate entity to another place you can specify checkpoint 'default' and entity will be spawned at this checkpoint.
        /// </summary>
        public string checkpointId;
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
        private SignalSource m_DisableSignalSource = default;
        private ObservableNullable<Entity> m_ObservableMaterialEntity = new();
        /// <summary>
        /// Determines if entity is being materialized right now
        /// </summary>
        public bool isBeingMaterialized { get; private set; } = false;
        
        public string gameAreaId
        {
            get => m_ObservableGameAreaId.value;
            set => m_ObservableGameAreaId.value = value;
        }
        
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

            if (GameArea.GetIntersectedGameArea(position, out var gameArea))
            {
                m_ObservableGameAreaId.value = gameArea.id;
            }
        }
        
        internal void InternalAwake()
        {
            this.place.world.RegisterVirtualEntity(this);
        }
        
        internal void InternalDestroy()
        {
            this.place.world.UnregisterVirtualEntity(this);
        }

        internal void Release()
        {
            place.ReleaseEntity(this);
        }
        
        public object Save()
        {
            return m_HasData
                ? new Entity.SaveData()
                {
                    blueprint = blueprint,
                    type = m_SaveData.type,
                    isStatic = isStatic,
                    isKicked = isKicked,
                    position = position,
                    rotation = m_SaveData.rotation,
                    gameAreaId = m_ObservableGameAreaId.value,
                    checkpointId = checkpointId,
                    dataComponents = m_SaveData.dataComponents
                }
                : null;
        }

        public UniTask Load(object data)
        {
            if (data is Entity.SaveData saveData)
            {
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

        protected async UniTaskVoid OnEnableAsync(Signal cancelSignal)
        {
            using var watchMaterialized = m_MaterializedEntity.WatchAsync(OnMaterializedChangeAsync);
            await cancelSignal;
            TryDematerialize();
        }
        
        private async UniTaskVoid OnMaterializedChangeAsync(Entity entity, bool hasEntity, Signal cancelSignal)
        {
            if (!hasEntity && !isKicked)
            {
                if (String.IsNullOrEmpty(checkpointId))
                {
                    // Should be materialized in the specified game area
                    using var observeGameAreaId = m_ObservableGameAreaId.WatchAsync(OnGameAreaIdChangeAsync);
                    await cancelSignal;
                }
                else
                {
                    // Should be materialized on the specified checkpoint
                }
                
            }
        }

        private async UniTaskVoid OnGameAreaIdChangeAsync(string gameAreaId, Signal cancelSignal)
        {
            if (String.IsNullOrEmpty(gameAreaId) || !GameArea.Exists(gameAreaId))
            {
                Console.Debug("VirtualEntity", "FindNewGameArea", id, $"Find new game area");
                // Find intersected game area
                if (GameArea.GetIntersectedGameArea(position, out var newArea))
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
                await cancelSignal;
            }
        }

        private UniTaskVoid OnRelatedGameAreaActivated(GameArea area, bool isActive, Signal cancellationSignal)
        {
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
                    if (GameArea.GetIntersectedGameArea(position, out var newArea))
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

        public void TryDematerialize()
        {
            if (m_MaterializedEntity.hasValue)
            {
                m_MaterializedEntity.value.Release();
            }
        }
        
        private async UniTaskVoid InternalMaterializeAsync(Signal cancellationSignal)
        {
            isBeingMaterialized = true;
            try
            {
                m_MaterializedEntity.value = await Entity.Spawn(blueprint, position,
                    Quaternion.identity, id, true, Save(), cancellationSignal);
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
    }
}