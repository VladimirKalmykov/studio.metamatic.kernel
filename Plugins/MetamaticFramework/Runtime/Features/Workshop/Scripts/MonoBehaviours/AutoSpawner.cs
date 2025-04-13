using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Metamatic.SaveSystem;
using UnityEngine;
using UnityEngine.Serialization;

namespace Metamatic.Framework.Workshop
{
    [Version("1.0.0")]
    public struct AutoSpawnerSaveData
    {
        public Dictionary<string, string> SpawnedEntities;
        public Dictionary<string, float> RespawnSchedulers;
        public float RespawnDelay;
    }
    public class VirtualAutoSpawner: VirtualEntityLogic
    {
        private AutoSpawnerSaveData data;
        private TimeService m_TimeService;
        public override async UniTaskVoid Run(Signal signal)
        {
            m_TimeService = await Service.Resolve<TimeService>(signal);
            Console.Debug("AutoSpawner", $"Run virtual logic with data: {data}");
            if (data.SpawnedEntities != null)
            {
                using var trackDelayer = PlayerLoop.LoopDelay(GetTrackingRandomDelay, TrackSpawnedEntities);
                await signal;
                Console.Debug("VirtualLogic", "AutoSpawner", "Stop logic");
            }
            
            Console.Debug("AutoSpawner", "Stop virtual logic");
        }

        private void TrackSpawnedEntities()
        {
            var removeList = ParamLessPool<List<string>>.GetShared();
            try
            {
                Console.Debug("AutoSpawner", $"Track spawned entities in background");
                var world = VirtualWorld.Current;
                // Track all spawned entities in the virtual world
                foreach (var pair in data.SpawnedEntities)
                {
                    var entityId = pair.Key;
                    var pointId = pair.Value;
                    if (!world.TryGetEntity(entityId, out var entity))
                    {
                        // Entity is not exists, remove it from the list
                        removeList.Add(entityId);

                        // Schedule respawn
                        Console.Debug("AutoSpawner", $"Virtual entity {entityId} not exists, schedule respawn");
                        data.RespawnSchedulers[pointId] = m_TimeService.time + data.RespawnDelay;
                    }
                }

                // Remove all entities that are not exists
                foreach (var entityId in removeList)
                {
                    data.SpawnedEntities.Remove(entityId);
                }
            }
            finally
            {
                removeList.Clear();
                ParamLessPool<List<string>>.ReturnShared(removeList);
            }
        }

        private float GetTrackingRandomDelay()
        {
            return UnityEngine.Random.Range(2f, 5f);
        }

        public override object Save()
        {
            Console.Debug("AutoSpawner", $"Save virtual logic");
            return data;
        }

        public override UniTask Load(object data)
        {
            if (data is AutoSpawnerSaveData saveData)
            {
                this.data = saveData;
            }
            return UniTask.CompletedTask;
        }
    }
    
    [RequireComponent(typeof(Entity))]
    [RequireService(typeof(TimeService))]
    public class AutoSpawner: EntityLogic<VirtualAutoSpawner>, ISavable
    {
        [SerializeField] private SpawnConfig m_SpawnConfig;
        [SerializeField] private bool m_RespawnOnDestroy = true;
        [SerializeField] private float m_RespawnDelay = 5f;
        
        public bool respawnOnDestroy => m_RespawnOnDestroy;
        public float respawnDelay => m_RespawnDelay;
        
        /// <summary>
        /// Map from entity id to spawn point id
        /// </summary>
        private Dictionary<string, string> m_SpawnedEntities = new();
        private Dictionary<string, float> m_RespawnSchedulers = new();
        
        private TimeService m_TimeService;
        
        protected override async UniTaskVoid Run(Signal signal)
        {
            m_TimeService = await Service.Resolve<TimeService>(signal);
            // Recover all schedulers
            if (m_RespawnSchedulers == null)
            {
                Console.Error("AutoSpawner", "Respawn schedulers is null");
                return;
            }
            foreach (var pair in m_RespawnSchedulers)
            {
                var spawnTime = pair.Value;
                if (m_TimeService.time < spawnTime)
                {
                    m_TimeService.ScheduleAtTime(spawnTime, () =>
                    {
                        m_RespawnSchedulers.Remove(pair.Key);
                        SpawnAtPoint(pair.Key, signal);
                    }, signal);
                }
            }
            
            Console.Debug("AutoSpawner", "Run material logic");
            foreach (var spawnPoint in m_SpawnConfig.spawnPoints)
            {
                if (!m_SpawnedEntities.ContainsValue(spawnPoint.id) && !m_RespawnSchedulers.ContainsKey(spawnPoint.id))
                {
                    SpawnAtPoint(spawnPoint, signal);
                }
            }

            await signal;
            Console.Debug("AutoSpawner", "Stop material logic");
        }

        private async UniTaskVoid TrackEntity(string entityId, Signal cancellationSignal)
        {
            try
            {
                await VirtualWorld.Current.WaitEntityNotExists(entityId, cancellationSignal);
                if (m_RespawnOnDestroy)
                {
                    if (m_SpawnedEntities.Remove(entityId, out var pointId))
                    {
                        ScheduleEntityAtPoint(pointId, cancellationSignal);
                    }
                }
            }
            catch (OperationCanceledException)
            {
              // Ignore  
            } catch (System.Exception e)
            {
                Console.Debug("AutoSpawner", $"Entity {entityId} not exists: {e}");
                return;
            }
        }

        private void ScheduleEntityAtPoint(string pointId, Signal cancellationSignal)
        {
            Console.Debug("AutoSpawner", $"Schedule entity at point {pointId}");
            var spawnTime = m_TimeService.time + m_RespawnDelay;
            m_RespawnSchedulers[pointId] = spawnTime;
            m_TimeService.ScheduleAtTime(spawnTime, () =>
            {
                m_RespawnSchedulers.Remove(pointId);
                SpawnAtPoint(pointId, cancellationSignal);
            }, cancellationSignal);
        }

        private void SpawnAtPoint(string pointId, Signal cancellationSignal)
        {
            Console.Debug("AutoSpawner", $"Spawn entity at point {pointId}");
            if (m_SpawnConfig.TryGetSpawnPoint(pointId, out var spawnPoint))
            {
                SpawnAtPoint(spawnPoint, cancellationSignal);
            }
            else
            {
                Console.Error("AutoSpawner", $"Spawn point {pointId} not found");
            }
        }

        private void SpawnAtPoint(Spawnpoint spawnPoint, Signal cancellationSignal)
        {
            var newEntity = VirtualWorld.Current.CreateEntity(spawnPoint.position, m_SpawnConfig.blueprint.AssetGUID);
            m_SpawnedEntities[newEntity.id] = spawnPoint.id;
            if (m_RespawnOnDestroy)
            {
                TrackEntity(newEntity.id, cancellationSignal).Forget();
            }
        }

        public object Save()
        {
            Console.Debug("AutoSpawner", $"Save material logic");
            return new AutoSpawnerSaveData
            {
                SpawnedEntities = m_SpawnedEntities,
                RespawnSchedulers = m_RespawnSchedulers,
                RespawnDelay = m_RespawnDelay
            };
        }

        public UniTask Load(object data)
        {
            Console.Debug("AutoSpawner", $"Load material logic");
            if (data is AutoSpawnerSaveData saveData)
            {
                m_SpawnedEntities = saveData.SpawnedEntities;
                m_RespawnSchedulers = saveData.RespawnSchedulers;
                return UniTask.CompletedTask;
            }
            else
            {
                throw new System.Exception($"Invalid data type: {data.GetType()}");
            }
        }
    }
}