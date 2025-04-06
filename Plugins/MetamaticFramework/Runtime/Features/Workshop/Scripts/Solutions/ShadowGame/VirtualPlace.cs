using System;
using System.Collections.Generic;
using System.Linq;
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
            public VirtualWorld.SaveServiceData[] services;
        }
        
        public string address;
        public object data;

        /// <summary>
        /// Means that all place data is placed to `data` property without extracting it to separate fields
        /// You can not interact with hibernated (e.g. add entities) place except saving and loading.
        /// </summary>
        public bool hibernated = true;
        
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
            if (hibernated)
            {
                GameSystem.FatalError(new Exception("Can't hibernate already hibernated place"));
                return;
            }
            hibernated = true;
            
            /* Release all entities */
            foreach (var pair in m_Entities.ToArray())
            {
                pair.Value.Release(DismissReason.Abandon);
            }
            
            this.data = Save();
            
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

            // Save services
            var services = new List<VirtualWorld.SaveServiceData>();
            foreach (var service in world.services)
            {
                var serviceData = service.SaveToPlace(address);
                if (serviceData != null)
                {
                    var serviceType = service.serviceType;
                    var saveServiceData = new VirtualWorld.SaveServiceData()
                    {
                        serviceType = serviceType,
                        data = serviceData
                    };
                }
            }
            saveData.services = services.ToArray();
            
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
                        ReleaseEntity(entity.id, DismissReason.Release);
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
                
                /* Load services */
                foreach (var serviceData in saveData.services)
                {
                    var service = world.GetOrCreateVirtualService(serviceData.serviceType);
                    if (service != null) // TODO: This condition is useless. Isn't it?
                    {
                        await service.LoadFromPlace(address, serviceData.data);
                    }
                    else
                    {
                        Console.Warning("VirtualPlace", $"Service {serviceData.serviceType} not found");
                    }
                }
            }
        }

        public VirtualEntity CreateEntity(string id, Vector3 position, string blueprintAddress, string checkpointId = null, Checkpoint checkpoint = null, bool dontEnable = false, bool alwaysPlayVirtualLogic = false) 
        {
            var entity = new VirtualEntity(this, id, position, blueprintAddress);
            entity.checkpoint = checkpoint;
            entity.checkpointId = checkpointId;
            entity.placeAddress = address;
            entity.alwaysPlayVirtualLogic = alwaysPlayVirtualLogic;
            entity.InternalAwake();
            m_Entities.Add(entity.id, entity);
            if (!dontEnable && !hibernated)
            {
                entity.enabled = true;
            }
            return entity;
        }

        public VirtualEntity CreateEntity(Entity.SaveData data)
        {
            var entity = new VirtualEntity(this, data.id, data.position, data.blueprint); // TODO: Use pool
            entity.Load(data).Forget();
            
            entity.InternalAwake();
            m_Entities.Add(entity.id, entity);
            if (!hibernated)
            {
                entity.enabled = true;
            }
            return entity;
        }

        public void ReleaseEntity(string id, DismissReason reason)
        {
            if (m_Entities.TryGetValue(id, out var entity))
            {
                ReleaseEntity(entity, reason);
            }
        }
        
        public void ReleaseEntity(VirtualEntity virtualEntity, DismissReason reason)
        {
            m_Entities.Remove(virtualEntity.id);
            virtualEntity.isAboutToDestroy = true;
            virtualEntity.enabled = false;
            virtualEntity.InternalDestroy(reason);
        }

        public void Dispose()
        {
            this.data = null;
            if (hibernated)
            {
                return;
            }
            hibernated = true;
            /* Dispose all entities */
            foreach (var pair in m_Entities)
            {
                pair.Value.InternalDestroy(DismissReason.Release);
            }
        }
        
        #region Private logic
        private async UniTask WakeAsync(Action ready, Signal cancellationSignal)
        {
            await Load(data);
            
            /* Enable all entities */
            foreach (var pair in m_Entities)
            {
                pair.Value.enabled = true;
            }

            await cancellationSignal;
        }
        #endregion
    }
}