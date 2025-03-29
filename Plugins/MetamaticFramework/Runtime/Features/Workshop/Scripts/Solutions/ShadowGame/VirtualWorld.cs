using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Metamatic.Framework.Workshop;
using Metamatic.SaveSystem;
using UnityEngine;
using UnityEngine.Serialization;

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
            public VirtualPlace.SaveData[] places;
            public SaveServiceData[] services;
        }
        
        private readonly Dictionary<string, VirtualPlace> m_Places = new();
        private readonly Dictionary<Type, VirtualService> m_Services = new();
        private readonly Dictionary<string, VirtualEntity> m_EntitiesRegistry = new();
        private readonly Dictionary<string, Action<VirtualEntity, bool>> m_EntityListeners = new();
        #if UNITY_EDITOR
        public readonly ObservableList<VirtualEntity> ObservableEntitiesList = new();
        #endif
        private SignalSource m_DisposeSignalSource;

        public VirtualWorld()
        {
            m_DisposeSignalSource = SignalSource.Create();
            RunAsync(m_DisposeSignalSource.Signal).Forget();
        }
        
        public void Virtualize(Entity entity)
        {
            var placeAddress = GameSystem.GetPlaceAddress(entity.gameObject);
            var virtualEntity = GetOrCreateVirtualEntity(placeAddress, entity.id, entity.transform.position, entity.blueprint);
            virtualEntity.Load(entity.Save());
        }
        
        public bool TryHydrate(Entity entity)
        {
            if (m_EntitiesRegistry.TryGetValue(entity.id, out var virtualEntity))
            {
                Console.Verbose("VirtualWorld", "Hydrate", $"Hydrating entity {entity.id}");
                entity.Load(virtualEntity.Save()).Forget();
                return true;
            }

            return false;
        }
        
        private async UniTaskVoid RunAsync(Signal cancellationSignal)
        {
            using var serviceSubscription = Service.WatchServices(OnServiceRegistered);
            using var placeWatcher = GameSystem.WatchPlaceActivationAsync(OnPlaceActivationAsync);
            await cancellationSignal;
        }

        public void RegisterVirtualEntity(VirtualEntity virtualEntity)
        {
            m_EntitiesRegistry[virtualEntity.id] = virtualEntity;
            if (m_EntityListeners.TryGetValue(virtualEntity.id, out var listener))
            {
                listener?.Invoke(virtualEntity, true);
            }
        }
        
        public void UnregisterVirtualEntity(VirtualEntity virtualEntity)
        {
            m_EntitiesRegistry.Remove(virtualEntity.id);
            if (m_EntityListeners.TryGetValue(virtualEntity.id, out var listener))
            {
                listener?.Invoke(virtualEntity, false);
            }
        }
        
        public DisposableAction WatchEntityExists(string id, Action<VirtualEntity, bool> listener)
        {
            m_EntityListeners[id] += listener;
            if (m_EntitiesRegistry.TryGetValue(id, out var virtualEntity))
            {
                listener?.Invoke(virtualEntity, true);
            }
            
            return DisposableAction.Create(() =>
            {
                m_EntityListeners[id] -= listener;
            });
        }
        
        public DisposableAction WatchEntityExistsAsync(string id, Func<VirtualEntity, bool, Signal, UniTaskVoid> listener)
        {
            var css = SignalSource.Empty;
            m_EntityListeners[id] += Handler;
            
            if (m_EntitiesRegistry.TryGetValue(id, out var virtualEntity))
            {
                css.TryRaise();
                css = SignalSource.Create();
                listener?.Invoke(virtualEntity, true, css.Signal);
            }
            
            return DisposableAction.Create(() =>
            {
                m_EntityListeners[id] -= Handler;
                css.Dispose();
            });

            void Handler(VirtualEntity entity, bool exists)
            {
                css.TryRaise();
                css = SignalSource.Create();
                listener?.Invoke(entity, exists, css.Signal);
            }
        }

        private void OnServiceRegistered(Service service)
        {
            /* Create virtual service for real service */
            GetOrCreateVirtualService(typeof(Service));
        }

        /// <summary>
        /// This handler is responsible for hibernating and waking up virtual places. It also creates virtual place if it does not exists.
        /// </summary>
        /// <param name="placeAddress"></param>
        /// <param name="cancellationSignal"></param>
        private async UniTaskVoid OnPlaceActivationAsync(string placeAddress, Signal cancellationSignal)
        {
            /* Create place if not exists */
            if (!m_Places.TryGetValue(placeAddress, out var place))
            {
                // TODO: Code duplication. Reuse.
                place = new VirtualPlace(this, placeAddress);
                place.address = placeAddress;
                m_Places.Add(placeAddress, place);
            }

            place.Wake();
            await cancellationSignal;
            place.Hibernate();
        }
        
        public VirtualPlace GetOrCreatePlace(string placeAddress)
        {
            if (!m_Places.TryGetValue(placeAddress, out var place))
            {
                place = new VirtualPlace(this, placeAddress);
                m_Places.Add(placeAddress, place);
            }

            return place;
        }

        public VirtualService GetOrCreateVirtualService(Type type)
        {
            if (!m_Services.TryGetValue(type, out var service))
            {
                service = new VirtualService(type);
                m_Services.Add(type, service);
            }

            return service;
        }
        
        public VirtualEntity CreateEntity(Vector3 position, string blueprintAddress)
        {
            var id = IdsGenerator.GenerateGuid();
            return CreateEntity(id, position, blueprintAddress);
        }
        
        public VirtualEntity CreateEntity(string id, Vector3 position, string blueprintAddress)
        {
            var placeAddress = GameSystem.activePlaceAddress;
            return CreateEntity(placeAddress, id, position, blueprintAddress);
        }

        public VirtualEntity CreateEntity(string placeAddress, string id, Vector3 position, string blueprintAddress)
        {
            // Resolve place
            if (!m_Places.TryGetValue(placeAddress, out var place))
            {
                place = new VirtualPlace(this, placeAddress);
                m_Places.Add(placeAddress, place);
            }

            return place.CreateEntity(id, position, blueprintAddress);
        }
        
        public bool RemoveEntity(string id)
        {
            if (m_EntitiesRegistry.TryGetValue(id, out var virtualEntity))
            {
                virtualEntity.Release();
                return true;
            }

            return false;
        }
        
        public VirtualEntity GetOrCreateVirtualEntity(string placeAddress, string id, Vector3 position, string blueprintAddress)
        {
            if (String.IsNullOrEmpty(placeAddress))
            {
                placeAddress = GameSystem.LimbPlaceAddress;
            }
            if (m_EntitiesRegistry.TryGetValue(id, out var virtualEntity))
            {
                virtualEntity.position = position;
                
                if (virtualEntity.blueprint != blueprintAddress)
                {
                    Console.Error("VirtualWorld", $"Inconsistent blueprint address for entity {id}. Expected {virtualEntity.blueprint}, but got {blueprintAddress}");
                }
                virtualEntity.blueprint = blueprintAddress;
                if (virtualEntity.placeAddress != placeAddress)
                {
                    Console.Error("VirtualWorld", $"Founded entity with id {id} has different place address {virtualEntity.placeAddress} than requested {placeAddress}");
                    // TODO: Translate entity to another place?
                }
                virtualEntity.placeAddress = placeAddress;
                return virtualEntity;
            }

            return CreateEntity(placeAddress, id, position, blueprintAddress);
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
            
            /* Force save to virtual world all material entities */
            foreach (var pairs in Entity.EntitiesRegistrationTokens)
            {
                if (Entity.EntitiesRegistry.TryGet(pairs.Value, out var entity))
                {
                    entity.Virtualize();
                } else {
                    Console.Error("EntitiesService", $"Unexpected behaviour. Entity with id {pairs.Key} was not found in the registry");
                }
            }

            var placeValues = m_Places.Values;
            var placesData = new VirtualPlace.SaveData[placeValues.Count];
            var j = 0;
            /* Collect virtual entities */
            foreach (var place in placeValues)
            {
                placesData[j] = (VirtualPlace.SaveData)(place.Save());
                j++;
            }

            var saveData = new SaveData
            {
                dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                services = servicesData.ToArray(),
                places = placesData
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
            
                /* Collect entity ids from active place */
                var activePlaceAddress = GameSystem.activePlaceAddress;
                VirtualPlace.SaveData? activePlaceData = null;
                var usedPlaceAddressed = new HashSet<string>();
                var loadTasks = new List<UniTask>(); // Use pool
                foreach (var place in saveData.places)
                {
                    usedPlaceAddressed.Add(place.address);
                    var virtualPlace = GetOrCreatePlace(place.address);
                    if (place.address == activePlaceAddress)
                    {
                        virtualPlace.Wake();
                    }
                    else
                    {
                        virtualPlace.Hibernate();
                    }
                    loadTasks.Add(virtualPlace.Load(place));
                }
            
                await UniTask.WhenAll(loadTasks);
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
                value.Dispose();
            }
            
            /* Dispose all places */
            foreach (var place in m_Places.ToArray())
            {
                var key = place.Key;
                var value = place.Value;
                m_Places.Remove(key);
                value.Dispose();
            }
        }
    }
}