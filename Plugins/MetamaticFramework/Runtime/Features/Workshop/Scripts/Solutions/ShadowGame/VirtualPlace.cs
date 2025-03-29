using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Metamatic.Exceptions;
using Metamatic.Framework;
using Metamatic.Framework.Workshop;
using Metamatic.SaveSystem;
using UnityEngine;

namespace Metamatic.Framework
{
    public class VirtualPlace: ISavable, IDisposable
    {
        [Serializable]
        [Version("1.0.0")]
        public struct SaveData
        {
            public string address;
            public Entity.SaveData[] entities;
        }
        
        public string address;
        public object data;
        /// <summary>
        /// Means that all place data is placed to `data` property without extracting it to separate fields
        /// You can not interact with hibernated (e.g. add entities) place except saving and loading.
        /// </summary>
        public bool hibernated;
        
        private readonly Dictionary<string, VirtualEntity> m_Entities = new();
        private SignalSource m_WakeCancellationSignalSource = default;
        public VirtualWorld world { get; private set; }

        public VirtualPlace(VirtualWorld world, string address)
        {
            this.world = world;
            this.address = address;
        }
        
        public void Hibernate()
        {
            if (hibernated) return;
            hibernated = true;
            this.data = Save();
            
            /* Release all entities */
            foreach (var pair in m_Entities)
            {
                pair.Value.InternalDestroy();
            }
            
            m_Entities.Clear();
        }
        
        public async UniTask Wake()
        {
            if (!hibernated)
            {
                GameSystem.FatalError(new Exception("Can't wake up non-hibernated place"));
                return;
            }
            hibernated = false;
            m_WakeCancellationSignalSource = SignalSource.Create();
            await new AsyncWork(WakeAsync, m_WakeCancellationSignalSource.Signal).Task;
        }
        
        public object Save()
        {
            if (hibernated)
            {
                return data;
            }
            var saveData = new SaveData()
            {
                address = address,
                entities = new Entity.SaveData[m_Entities.Count]
            };
            
            var i = 0;
            foreach (var pair in m_Entities)
            {
                saveData.entities[i] = (Entity.SaveData)pair.Value.Save();
                i++;
            }
            
            return saveData;
        }
        
        public async UniTask Load(object data)
        {
            if (hibernated)
            {
                this.data = data;
                return;
            }
            if (data is SaveData saveData)
            {
                if (this.address != saveData.address)
                {
                    Console.Error("VirtualPlace", $"Inconsistent address: {this.address} != {saveData.address}");
                    GameSystem.FatalError(new Exception("Inconsistent address"));
                    return;
                }
                var spawnEntitiesIds = new HashSet<string>();
            
                /* Collect entity ids */
                foreach (var entitySaveData in saveData.entities)
                {
                    spawnEntitiesIds.Add(entitySaveData.id);
                }
                
                var existingEntityIds = new HashSet<string>(m_Entities.Keys);
            
                var asyncTasks = new List<UniTask>();
                /* Farewell, old material entities */
                foreach (var pair in m_Entities)
                {
                    var entity = pair.Value;
                    if (entity.isStatic)
                    {
                        existingEntityIds.Add(entity.id);
                        return;
                    }
                    // Remove only those entities that are not in the save data
                    if (!spawnEntitiesIds.Contains(entity.id))
                    {
                        asyncTasks.Add(ReleaseEntity(entity.id));
                    }
                    else
                    {
                        existingEntityIds.Add(entity.id);
                    }
                }
                await UniTask.WhenAll(asyncTasks);

                foreach (var entitySaveData in saveData.entities)
                {
                    if (!existingEntityIds.Contains(entitySaveData.id))
                    {
                        CreateEntity(entitySaveData);
                    }
                }
            }
        }

        public VirtualEntity CreateEntity(string id, Vector3 position, string blueprintAddress) 
        {
            var entity = new VirtualEntity(this, id, position, blueprintAddress);
            entity.InternalAwake();
            m_Entities.Add(entity.id, entity);
            return entity;
        }

        public VirtualEntity CreateEntity(Entity.SaveData data)
        {
            var entity = new VirtualEntity(this, data.id, data.position, data.blueprint); // TODO: Use pool
            entity.Load(data).Forget();
            
            entity.InternalAwake();
            m_Entities.Add(entity.id, entity);
            return entity;
        }

        public void ReleaseEntity(string id)
        {
            if (m_Entities.TryGetValue(id, out var entity))
            {
                ReleaseEntity(entity);
            }
        }
        
        public void ReleaseEntity(VirtualEntity entity)
        {
            entity.enabled = false;
            entity.InternalDestroy();
            m_Entities.Remove(entity.id);
        }

        public void Dispose()
        {
            this.data = null;
            if (hibernated)
            {
                return;
            }
            /* Dispose all entities */
            foreach (var pair in m_Entities)
            {
                pair.Value.InternalDestroy();
            }
        }
        
        #region Private logic
        private async UniTask WakeAsync(Action ready, Signal cancellationSignal)
        {
            await Load(data);
            
            /* Enable all entities */
            foreach (var pair in m_Entities)
            {
                pair.Value.InternalEnable();
            }

            await cancellationSignal;
        }
        #endregion
    }
}