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
    public class VirtualPlace : IWorldSavable, IDisposable
    {
        [Serializable]
        [Version("1.0.0")]
        public struct SaveData
        {
            public string address;
            public Entity.SaveData[] entities;
            public ServiceSaveData[] services;
        }

        public string address;
        public SaveData data;

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
            this.data = new SaveData()
            {
                address = address,
                entities = Array.Empty<Entity.SaveData>(),
                services = Array.Empty<ServiceSaveData>()
            };
        }

        public void TryHibernate()
        {
            if (hibernated)
            {
                return;
            }

            Hibernate();
        }
        
        public void Hibernate()
        {
            if (hibernated)
            {
                GameSystem.FatalError(new Exception("Can't hibernate already hibernated place"));
                return;
            }

            this.data = (SaveData)SaveToWorld();
            hibernated = true;

            /* Release all entities */
            foreach (var pair in m_Entities.ToArray())
            {
                ReleaseEntity(pair.Value, DismissReason.Abandon);
            }

            m_Entities.Clear();
        }

        public async UniTask TryAwake()
        {
            if (hibernated)
            {
                await Wake();
            }
        }

        public async UniTask Wake()
        {
            if (!hibernated)
            {
                Console.Warning("VirtualPlace", "Can't wake up non-hibernated place");
                return;
            }

            hibernated = false;
            m_WakeCancellationSignalSource = SignalSource.Create();
            await new AsyncWork(WakeAsync, m_WakeCancellationSignalSource.Signal).Task;
        }

        /// <summary>
        /// Finds entity by id and returns its descriptor.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool TryFindEntity(string id, out EntityDescriptor entity)
        {
            if (hibernated)
            {
                foreach (var entitySaveData in this.data.entities)
                {
                    if (entitySaveData.id == id)
                    {
                        entity = new EntityDescriptor(world, this, entitySaveData.id, true, false, entitySaveData);
                        return true;
                    }
                }
            }
            else
            {
                if (m_Entities.TryGetValue(id, out var virtualEntity))
                {
                    entity = new EntityDescriptor(world, this, virtualEntity.id, false, virtualEntity.isMaterialized,
                        (Entity.SaveData)virtualEntity.Save());
                    return true;
                }
            }

            entity = default;
            return false;
        }

        public object SaveToWorld()
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
            var services = new List<ServiceSaveData>();
            foreach (var service in world.services)
            {
                var serviceData = service.SaveToPlace(address);
                if (serviceData != null)
                {
                    var serviceType = service.serviceType;
                    var saveServiceData = new ServiceSaveData()
                    {
                        serviceType = serviceType,
                        data = serviceData
                    };
                }
            }

            saveData.services = services.ToArray();

            return saveData;
        }

        public async UniTask LoadFromWorld(object data)
        {
            if (data is SaveData saveData)
            {
                if (hibernated)
                {
                    this.data = saveData;
                    return;
                }

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
                        CreateVirtualEntity(entitySaveData);
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
            else
            {
                Console.Error("VirtualPlace", "Can't load place. Data is not SaveData");
                GameSystem.FatalError(new Exception("Can't load place. Data is not SaveData\""));
                return;
            }
        }

        public VirtualEntity CreateVirtualEntity(string id, Vector3 position, string blueprintAddress,
            string checkpointId = null, Checkpoint checkpoint = null, bool dontEnable = false,
            bool alwaysPlayVirtualLogic = false)
        {
            var entity = new VirtualEntity(this, id, position, blueprintAddress)
            {
                checkpoint = checkpoint,
                checkpointId = checkpointId,
                placeAddress = address,
                alwaysPlayVirtualLogic = alwaysPlayVirtualLogic
            };
            entity.InternalAwake();
            m_Entities.Add(entity.id, entity);
            if (!dontEnable && !hibernated)
            {
                entity.enabled = true;
            }

            return entity;
        }

        public VirtualEntity CreateVirtualEntity(Entity.SaveData data)
        {
            var entity = new VirtualEntity(this, data.id, data.position, data.blueprint); // TODO: Use pool
            entity.placeAddress = address;
            entity.Load(data).Forget();

            entity.InternalAwake();
            m_Entities.Add(entity.id, entity);
            if (!hibernated)
            {
                entity.enabled = true;
            }

            return entity;
        }

        public bool ReleaseEntity(string id, DismissReason reason)
        {
            if (hibernated)
            {
                if (data is SaveData saveData)
                {
                    // Remove entity from hibernated data
                    var entities = saveData.entities.ToList();
                    for (var i = 0; i < entities.Count; i++)
                    {
                        if (entities[i].id == id)
                        {
                            entities.RemoveAt(i);
                            return true;
                        }
                    }

                    saveData.entities = entities.ToArray();
                }

                Console.Warning("VirtualPlace", $"Can't release non-existed entity {id}");
                return false;
            }

            if (m_Entities.TryGetValue(id, out var entity))
            {
                ReleaseEntity(entity, reason);
                return true;
            }

            return false;
        }

        public void ReleaseEntity(VirtualEntity virtualEntity, DismissReason reason)
        {
            m_Entities.Remove(virtualEntity.id);
            virtualEntity.isAboutToDestroy = true;
            virtualEntity.enabled = false;
            virtualEntity.InternalDestroy();
        }

        public void CreateEntity(Entity.SaveData entityData)
        {
            if (hibernated)
            {
                var entities = this.data.entities.ToList();
                entities.Add(entityData);
                this.data.entities = entities.ToArray();
            }
            else
            {
                CreateVirtualEntity(entityData);
            }
        }

        public void Dispose()
        {
            this.data = default;
            if (hibernated)
            {
                return;
            }

            hibernated = true;

            /* Stop all entities */
            foreach (var pair in m_Entities)
            {
                pair.Value.InternalStop();
            }

            /* Disable all entities */
            foreach (var pair in m_Entities.ToArray())
            {
                // Need to mark as 'isAboutToDestroy' to prevent immediate dematerialize with virtualization
                pair.Value.isAboutToDestroy = true;
                pair.Value.enabled = false;
            }

            /* Dispose all entities */
            foreach (var pair in m_Entities.ToArray())
            {
                ReleaseEntity(pair.Value, DismissReason.Release);
            }
        }

        #region Private logic

        private async UniTask WakeAsync(Action ready, Signal cancellationSignal)
        {
            await LoadFromWorld(data);

            /* Enable all entities */
            foreach (var pair in m_Entities)
            {
                pair.Value.enabled = true;
            }

            using (PlayerLoop.LoopDelay(10f, ClearUpExpiredEntities))
            {
                await cancellationSignal;
            }
        }

        private void ClearUpExpiredEntities()
        {
            // TODO: Cache time service
            if (Service.TryResolveSync<TimeService>(out var timeService))
            {
                // TODO: Maybe use pool for expired entities? Or at least keep them in a separate list?
                /* Iterate over all entities */
                foreach (var pair in m_Entities.ToArray())
                {
                    var entity = pair.Value;
                    if (entity.expirationTime == 0f || entity.expirationTime > timeService.time)
                    {
                        continue;
                    }
                    // Release expired entity
                    entity.Release(DismissReason.Release);
                }
            }
        }

        #endregion
    }
}