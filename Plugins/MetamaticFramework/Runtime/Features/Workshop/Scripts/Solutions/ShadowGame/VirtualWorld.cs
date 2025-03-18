using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Metamatic.Framework.Workshop;
using Metamatic.SaveSystem;

namespace Metamatic.Framework
{
    public class VirtualWorld: ISavable, IDisposable
    {
        [Serializable]
        [Version("1.0.0")]
        public class SaveServiceData
        {
            public Type serviceType;
            public Type dataType;
            public object data;
        }

        [Serializable]
        [Version("1.0.0")]
        public struct SaveData
        {
            public string dateTime;
            public SaveServiceData[] services;
            public Entity.SaveData[] entities;
        }
        
        private readonly Dictionary<Type, VirtualService> m_Services = new();
        private readonly Dictionary<(Type, string), VirtualEntity> m_Entities = new();
        #if UNITY_EDITOR
        public readonly ObservableList<VirtualEntity> ObservableEntitiesList = new();
        #endif
        private SignalSource m_DisposeSignalSource;

        public VirtualWorld()
        {
            m_DisposeSignalSource = SignalSource.Create();
            RunAsync(m_DisposeSignalSource.Signal).Forget();
        }
        
        public void VirtualizeEntity(Entity entity)
        {
            var virtualEntity = GetOrAddVirtualEntity(entity.GetType(), entity.id);
            virtualEntity.Load(entity.Save());
        }
        
        public async UniTaskVoid MaterializeEntity(VirtualEntity virtualEntity)
        {
            var entity = await Entity.SpawnEntity(virtualEntity.blueprint, virtualEntity.position, virtualEntity.rotation, id: virtualEntity.id);
            /* Remove virtual entity from the registry */
            
            m_Entities.Remove((entity.GetType(), entity.id));
#if UNITY_EDITOR
            ObservableEntitiesList.Remove(virtualEntity);
#endif
            await entity.Load(virtualEntity.Save());
        }
        
        private async UniTaskVoid RunAsync(Signal cancellationSignal)
        {
            using var serviceSubscription = Service.WatchServices(OnServiceRegistered);
            using var areasActivationSubscription = GameArea.WatchAreasActivation(OnAreaActivation);
            await cancellationSignal;
        }

        private void OnAreaActivation(GameArea gameArea)
        {
            // TODO: Consider to perform iteration async with UniTask.Yield() or UniTask.DelayFrame(); 
            /* Iterate over all virtualized entities */
            foreach (var virtualEntity in m_Entities.Values)
            {
                if (virtualEntity.origin == null)
                {
                    /* If entity is virtual, check if it is inside the active area */
                    if (virtualEntity.gameAreaId == gameArea.id)
                    {
                        if (GameArea.IsIntersectedWithActiveAreas(virtualEntity.position))
                        {
                            if (virtualEntity.isStatic)
                            {
                                // Do nothing, because static entities will find its virtual entity in the registry by itself
                                continue;
                            }
                            /* If entity is inside the active area, spawn it */
                            MaterializeEntity(virtualEntity).Forget();
                        }
                    }
                }
            }
        }

        private void OnServiceRegistered(Service service)
        {
            /* Create virtual service for real service */
            GetOrCreateVirtualService(typeof(Service));
        }

        public VirtualService GetOrCreateVirtualService(Type type)
        {
            if (!m_Services.TryGetValue(type, out var service))
            {
                service = new VirtualService(this, type);
                service.InternalAwake();
                m_Services.Add(type, service);
            }

            return service;
        }
        
        public VirtualEntity GetOrAddVirtualEntity(Type type, string id)
        {
            if (m_Entities.TryGetValue((type, id), out var virtualEntity))
            {
                return virtualEntity;
            }

            virtualEntity = new VirtualEntity(id); // TODO: Probably it may be derived class of VirtualEntity
            m_Entities[(type, id)] = virtualEntity;
#if UNITY_EDITOR
            ObservableEntitiesList.Add(virtualEntity);
#endif
            return virtualEntity;
        }

        public object Save()
        {
            /* Save services data */
            var servicesData = new List<SaveServiceData>();
            /* Walk services and get its data */
            foreach (var service in m_Services.Values)
            {
                if (service is ISavable savable)
                {
                    var data = savable.Save();
                    if (data != null)
                    {
                        servicesData.Add(new SaveServiceData
                        {
                            serviceType = service.serviceType, // TODO: Проблема обратного преобразования, если сервис переименуется
                            dataType = data.GetType(), // TODO: Рудимент, тип данных сохраняется через конвертер (но нужно проверить)
                            data = data
                        });
                    }
                }
            }
            
            /* Save entities data */
            var entitiesData = new Dictionary<string, Entity.SaveData>(); // TODO: Reuse list
            
            /* Collect material entities */
            foreach (var pairs in Entity.EntitiesRegistrationTokens)
            {
                if (Entity.EntitiesRegistry.TryGet(pairs.Value, out var entity))
                {
                    entitiesData[entity.id] = (Entity.SaveData)(entity.Save());
                } else {
                    Console.Error("EntitiesService", $"Unexpected behaviour. Entity with id {pairs.Key} was not found in the registry");
                }
            }
            
            /* Collect virtual entities */
            foreach (var virtualEntity in m_Entities.Values)
            {
                if (virtualEntity.origin == null) // TODO: Instad of compare to null, we can use some kind of flag
                {
                    entitiesData[virtualEntity.id] = (Entity.SaveData)(virtualEntity.Save());
                }
            }

            var saveData = new SaveData
            {
                dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                services = servicesData.ToArray(),
                entities = entitiesData.Values.ToArray()
            };

            return saveData;
        }

        public async UniTask Load(object data)
        {
            if (data is SaveData saveData)
            {
                /* Load services */
                var tasks = new List<UniTask>();
                foreach (var serviceData in saveData.services)
                {
                    if (Service.Services.TryGetValue(serviceData.serviceType, out var service))
                    {
                        if (service is ISavable savable)
                        {
                            tasks.Add(savable.Load(serviceData.data));
                        }
                    }
                    else
                    {
                        Console.Error("Launcher", $"Service not found: {serviceData.serviceType}");
                        var virtualService = GetOrCreateVirtualService(serviceData.serviceType).Load(serviceData.data);
                    }
                }
                await UniTask.WhenAll(tasks);
                
                var spawnEntitiesIds = new HashSet<string>();
            
                /* Collect entity ids */
                foreach (var entitySaveData in saveData.entities)
                {
                    spawnEntitiesIds.Add(entitySaveData.id);
                }
            
                /* Farewell, old material entities */
                foreach (var pairs in Entity.EntitiesRegistrationTokens)
                {
                    if (Entity.EntitiesRegistry.TryGet(pairs.Value, out var entity))
                    {
                        if (entity.isStatic)
                        {
                            return;
                        }
                        // Remove only those entities that are not in the save data
                        if (!spawnEntitiesIds.Contains(entity.id))
                        {
                            entity.Release();
                        }
                    }
                }

                var loadTasks = new List<UniTask>();
                foreach (var entitySaveData in saveData.entities)
                {
                    loadTasks.Add(LoadEntity(entitySaveData));
                }
            
                await UniTask.WhenAll(loadTasks);
            }
        }
        
        private async UniTask LoadEntity(Entity.SaveData entitySaveData)
        {
            Entity entity = null;
            
            // TODO: Crate Entity.RegisteredEntities property
            if (Entity.EntitiesRegistrationTokens.TryGetValue(entitySaveData.id, out var token))
            {
                if (Entity.EntitiesRegistry.TryGet(token, out entity))
                {
                    // Ok, entity is found in the registry
                    await entity.Load(entitySaveData);
                    return;
                }
            }

            /* If entity is not found in the registry, try to spawn it or crate virtual entity */
            if (entity == null)
            {
                if (entitySaveData.isStatic)
                {
                    /* If entity is static, we can create virtual entity, because it is a part of the some scene and should not be spawned */
                    await GetOrAddVirtualEntity(entitySaveData.type, entitySaveData.id).Load(entitySaveData);
                }
                else
                {
                    var entityPosition = entitySaveData.position;
                    if (GameArea.IsIntersectedWithActiveAreas(entityPosition))
                    {
                        /* If entity is inside the game area, we can spawn it */
                        entity = await Entity.SpawnEntity(entitySaveData.blueprint, entitySaveData.position,
                            entitySaveData.rotation, id: entitySaveData.id, activate: false);
                        await entity.Load(entitySaveData);
                    }
                    else
                    {
                        /* Otherwise, virtualize entity */
                        GetOrAddVirtualEntity(entitySaveData.type, entitySaveData.id).Load(entitySaveData);
                    }
                }
            }
        }

        public void Dispose()
        {
            // TODO: Maybe use one common signal source for all services?
            /* Dispose all services */
            foreach (var service in m_Services.ToArray())
            {
                var key = service.Key;
                var value = service.Value;
                m_Services.Remove(key);
                value.enabled = false;
                value.Dispose();
            }
        }
    }
}