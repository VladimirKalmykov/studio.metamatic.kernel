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
        
        public static VirtualWorld Current { get; private set; }
        
        public static VirtualWorld CreateNewCurrent()
        {
            if (Current != null)
            {
                Current.Dispose();
            }
            Current = new VirtualWorld();
            return Current;
        }

        public static void DisposeCurrent()
        {
            if (Current != null)
            {
                Current.Dispose();
                Current = null;
            }
        }
        
        private readonly Dictionary<string, VirtualPlace> m_Places = new();
        private readonly Dictionary<Type, VirtualService> m_Services = new();
        private readonly Dictionary<string, VirtualEntity> m_EntitiesRegistry = new();
        private readonly Dictionary<string, MicroEvent<VirtualEntity, bool>> m_EntityListeners = new();
        #if UNITY_EDITOR
        public readonly ObservableList<VirtualEntity> ObservableEntitiesList = new();
        #endif
        private SignalSource m_DisposeSignalSource;

        public VirtualWorld()
        {
            m_DisposeSignalSource = SignalSource.Create();
            RunAsync(m_DisposeSignalSource.Signal).Forget();
        }
        
        public bool HasEntity(string id)
        {
            return m_EntitiesRegistry.ContainsKey(id);
        }
        
        public bool ReleaseEntity(string id, DismissReason reason)
        {
            if (m_EntitiesRegistry.TryGetValue(id, out var entity))
            {
                entity.Release(reason);
                return true;
            }

            return false;
        }
        
        public bool TryGetEntity(string id, out VirtualEntity entity)
        {
            return m_EntitiesRegistry.TryGetValue(id, out entity);
        }
        
        /// <summary>
        /// Virtualize entity. It will be saved to virtual world and can be restored later.
        /// Material entity will not be removed. To remove it use entity.Dematerialize method.
        /// </summary>
        /// <param name="entity"></param>
        public void Virtualize(Entity entity)
        {
            var placeAddress = GameSystem.GetCurrentPlaceAddress();
            var virtualEntity = GetOrCreateVirtualEntity(placeAddress, entity.id, entity.transform.position, entity.blueprint);
            virtualEntity.Load(entity.Save());
        }
        
        public bool TryHydrate(Entity entity)
        {
            if (!m_EntitiesRegistry.TryGetValue(entity.id, out var virtualEntity))
            {
                // Create virtual entity
                virtualEntity = CreateEntity(GameSystem.activePlaceAddress, entity.id, entity.transform.position, entity.blueprint, dontEnable: true);
                virtualEntity.Load(entity.Save()).Forget();
            }
            else
            {
                GameSystem.HandleExceptionsFrom(entity.Load(virtualEntity.Save()));
            }
            entity.InternalBindToVirtualEntity(virtualEntity);
            virtualEntity.InternalBindToMaterializedEntity(entity);
            virtualEntity.EnableIfNotHybernated();
            Console.Verbose("VirtualWorld", "Hydrate", $"Hydrating entity {entity.id}");
            return true;
        }
        
        private async UniTaskVoid RunAsync(Signal cancellationSignal)
        {
            using var serviceSubscription = Service.WatchServices(OnServiceRegistered);
            using var placeWatcher = GameSystem.WatchPlaceActivationAsync(OnPlaceActivationAsync);
            await cancellationSignal;
        }

        public void InternalRegisterVirtualEntity(VirtualEntity virtualEntity)
        {
            m_EntitiesRegistry[virtualEntity.id] = virtualEntity;
            if (m_EntityListeners.TryGetValue(virtualEntity.id, out var listener))
            {
                listener.Emit(virtualEntity, true);
            }
        }
        
        public void InternalUnregisterVirtualEntity(VirtualEntity virtualEntity)
        {
            m_EntitiesRegistry.Remove(virtualEntity.id);
            if (m_EntityListeners.TryGetValue(virtualEntity.id, out var listener))
            {
                listener.Emit(virtualEntity, false);
            }
        }
        
        
        public DisposableAction WatchEntityExists(string id, Action<VirtualEntity, bool> listener)
        {
            if (!m_EntityListeners.TryGetValue(id, out var listeners))
            {
                listeners = new MicroEvent<VirtualEntity, bool>();
                m_EntityListeners.Add(id, listeners);
            }

            var disposeListener = listeners.Subscribe(listener);
            if (m_EntitiesRegistry.TryGetValue(id, out var virtualEntity))
            {
                listener?.Invoke(virtualEntity, true);
            }
            
            return DisposableAction.Create(() =>
            {
                disposeListener.Dispose();
                if (listeners.isEmpty)
                {
                    m_EntityListeners.Remove(id);
                }
            });
        }
        
        public DisposableAction WatchEntityExistsAsync(string id, Func<VirtualEntity, bool, Signal, UniTaskVoid> listener)
        {
            var css = SignalSource.Empty;
            if (!m_EntityListeners.TryGetValue(id, out var listeners))
            {
                listeners = new MicroEvent<VirtualEntity, bool>();
                m_EntityListeners.Add(id, listeners);
            }
            
            var disposeListener = listeners.Subscribe(Handler);
            
            if (m_EntitiesRegistry.TryGetValue(id, out var virtualEntity))
            {
                css.TryRaise();
                css = SignalSource.Create();
                listener?.Invoke(virtualEntity, true, css.Signal);
            }
            else
            {
                listener?.Invoke(null, false, css.Signal);
            }
            
            return DisposableAction.Create(() =>
            {
                disposeListener.Dispose();
                if (listeners.isEmpty)
                {
                    m_EntityListeners.Remove(id);
                }
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

            place.Wake().Forget(); // TODO: Check if it is necessary to await
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

        public VirtualEntity CreateEntity(
            Vector3 position,
            string blueprintAddress
        )
        {
            return CreateEntity(GameSystem.GetCurrentPlaceAddress(), IdsGenerator.GenerateGuid(), position, blueprintAddress);
        }

        public VirtualEntity CreateEntity(
            string placeAddress,
            string id,
            Vector3 position,
            string blueprintAddress,
            string checkpointId = null,
            Checkpoint checkpoint = null,
            bool dontEnable = false
        )
        {
            // Resolve place
            if (!m_Places.TryGetValue(placeAddress, out var place))
            {
                place = new VirtualPlace(this, placeAddress);
                m_Places.Add(placeAddress, place);
            }

            return place.CreateEntity(id, position, blueprintAddress, checkpointId: checkpointId, checkpoint: checkpoint, dontEnable: dontEnable);
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
                        await virtualPlace.Wake();
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