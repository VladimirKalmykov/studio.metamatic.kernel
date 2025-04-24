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
    public class VirtualWorld: IDisposable
    {
        [Serializable]
        [Version("1.0.0")]
        public struct SaveData
        {
            public string dateTime;
            public VirtualPlace.SaveData[] places;
            public ServiceSaveData[] services;
        }
        
        private static readonly Observable<VirtualWorld> s_ObservableCurrentVirtualWorld = new Observable<VirtualWorld>();
        public static VirtualWorld Current => s_ObservableCurrentVirtualWorld.value;
        
        public static VirtualWorld CreateNewCurrent()
        {
            if (Current != null)
            {
                Current.Dispose();
            }
            s_ObservableCurrentVirtualWorld.value = new VirtualWorld();
            return Current;
        }

        public static void DisposeCurrent()
        {
            if (s_ObservableCurrentVirtualWorld.value != null)
            {
                s_ObservableCurrentVirtualWorld.value.Dispose();
                s_ObservableCurrentVirtualWorld.value = null;
            }
        }

        public static DisposableAction WatchVirtualWorldAsync(Func<VirtualWorld, Signal, UniTaskVoid> action)
        {
            return s_ObservableCurrentVirtualWorld.WatchAsync(action);
        }
        
        private readonly Dictionary<string, VirtualPlace> m_Places = new();
        private readonly Dictionary<Type, VirtualService> m_Services = new();
        private readonly Dictionary<string, VirtualEntity> m_EntitiesRegistry = new();
        private readonly Dictionary<string, MicroEvent<VirtualEntity, bool>> m_EntityListeners = new();
        #if UNITY_EDITOR
        public readonly ObservableList<VirtualEntity> DebugObservableEntitiesList = new();
        #endif
        private SignalSource m_DisposeSignalSource;
        private bool m_Disposed = false;
        
        // Get ability to iterate over all servies
        public IEnumerable<VirtualService> services => m_Services.Values;

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
        
        /// <summary>
        /// Try release dematerialized entity in current place. It will be removed from virtual world and current place if exists and is not materialized.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public bool TryReleaseVirtualEntityInCurrentPlaceIfNotMaterialized(string id, DismissReason reason)
        {
            if (m_EntitiesRegistry.TryGetValue(id, out var entity))
            {
                if (!entity.isMaterialized)
                {
                    entity.Release(reason);
                }
                
                return true;
            }

            return false;
        }
        
        public async UniTask TryHydrate(Entity entity)
        {
            if (!m_EntitiesRegistry.TryGetValue(entity.id, out var virtualEntity))
            {
                // Create virtual entity
                virtualEntity = CreateEntity(GameSystem.activePlaceAddress, entity.id, entity.transform.position, entity.blueprint, dontEnable: true, alwaysPlayVirtualLogic: entity.alwaysPlayVirtualLogic);
                if (entity.alwaysPlayVirtualLogic)
                {
                    await virtualEntity.Load(entity.Save());
                }
                entity.InternalMarkMaterialized();
            }
            else
            {
                await entity.Materialize(virtualEntity.Save());
            }
            entity.InternalBindToVirtualEntity(virtualEntity);
            virtualEntity.InternalBindToMaterializedEntity(entity);
            virtualEntity.EnableIfNotHybernated();
            Console.Verbose("VirtualWorld", "Hydrate", $"Hydrating entity {entity.id}");
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
#if DEBUG
            DebugObservableEntitiesList.Add(virtualEntity);
#endif
            if (m_EntityListeners.TryGetValue(virtualEntity.id, out var listener))
            {
                listener.Emit(virtualEntity, true);
            }
        }
        
        public void InternalUnregisterVirtualEntity(VirtualEntity virtualEntity)
        {
            m_EntitiesRegistry.Remove(virtualEntity.id);
#if DEBUG
            DebugObservableEntitiesList.Remove(virtualEntity);
#endif
            if (m_EntityListeners.TryGetValue(virtualEntity.id, out var listener))
            {
                listener.Emit(virtualEntity, false);
            }
        }
        
        /// <summary>
        /// Listener will be triggered when entity with id is created or destroyed.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="listener"></param>
        /// <returns></returns>
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
            else
            {
                listener?.Invoke(null, false);
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
        
        public DisposableAction WatchVirtualEntityExistsAsync(string id, Func<VirtualEntity, bool, Signal, UniTaskVoid> listener)
        {
            var css = SignalSource.Create();
            if (!m_EntityListeners.TryGetValue(id, out var listeners))
            {
                listeners = new MicroEvent<VirtualEntity, bool>();
                m_EntityListeners.Add(id, listeners);
            }
            
            var disposeListener = listeners.Subscribe(Handler);
            
            if (m_EntitiesRegistry.TryGetValue(id, out var virtualEntity))
            {
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
        
        public UniTask WaitEntityNotExists(string id, Signal cancellationSignal)
        {
            if (!m_EntitiesRegistry.ContainsKey(id))
            {  
                return UniTask.CompletedTask;
            }
            var tcs = SafeUniTaskCompletionSource.Create();
            
            if (!m_EntityListeners.TryGetValue(id, out var listeners))
            {
                listeners = new MicroEvent<VirtualEntity, bool>();
                m_EntityListeners.Add(id, listeners);
            }

            Action onDispose = null;
            
            onDispose += listeners.Subscribe(Handler);
            onDispose += cancellationSignal.Register(() =>
            {
                onDispose?.Invoke();
                tcs.TrySetCanceled();
            });
            
            return tcs.Task;
            
            void Handler(VirtualEntity entity, bool exists)
            {
                if (!exists)
                {
                    tcs.TrySetResult();
                    onDispose?.Invoke();
                }
            }
        }

        private void OnServiceRegistered(Service service)
        {
            /*
             * Create virtual service only for those services that are savable.
             */
            if (service is IWorldSavable or IPlaceSavable)
            {
                /* Create virtual service for real service */
                GetOrCreateVirtualService(typeof(Service));
            }
        }

        /// <summary>
        /// This handler is responsible for hibernating and waking up virtual places. It also creates virtual place if it does not exists.
        /// </summary>
        /// <param name="placeAddress"></param>
        /// <param name="cancellationSignal"></param>
        private async UniTaskVoid OnPlaceActivationAsync(string placeAddress, Signal cancellationSignal)
        {
            if (String.IsNullOrEmpty(placeAddress)) return;
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
            if (!m_Disposed && GameSystem.isGameRunning)
            {
                place.Hibernate();
            }
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
            bool dontEnable = false,
            bool alwaysPlayVirtualLogic = false
        )
        {
            // Resolve place
            if (!m_Places.TryGetValue(placeAddress, out var place))
            {
                place = new VirtualPlace(this, placeAddress);
                m_Places.Add(placeAddress, place);
            }

            return place.CreateVirtualEntity(id, position, blueprintAddress, checkpointId: checkpointId, checkpoint: checkpoint, dontEnable: dontEnable, alwaysPlayVirtualLogic: alwaysPlayVirtualLogic);
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

        /// <summary>
        /// Find entity in all places.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="entityDescriptor"></param>
        /// <returns></returns>
        public bool TryFindEntity(string id, out EntityDescriptor entityDescriptor)
        {
            if (!TryGetEntity(id, out var virtualEntity))
            {
                // Entity not found in current place. Try to find it in all places.
                foreach (var place in m_Places.Values)
                {
                    if (place.TryFindEntity(id, out entityDescriptor))
                    {
                        return true;
                    }
                }
            }
            else
            {
                entityDescriptor = new EntityDescriptor(this, virtualEntity.place, virtualEntity.id, false,
                    virtualEntity.isMaterialized, (Entity.SaveData)virtualEntity.Save());
                return true;
            }

            entityDescriptor = default;
            return false;
        }

        public bool MoveEntityToPlace(string entityId, string placeAddress)
        {
            if (TryFindEntity(entityId, out var entityDescriptor))
            {
                if (placeAddress != entityDescriptor.place.address)
                {
                    var nextPlace = GetOrCreatePlace(placeAddress);
                    var nextData = entityDescriptor.data;
                    nextData.checkpointId = "default";
                    nextData.gameAreaId = null;
                    nextPlace.CreateEntity(nextData);
                    entityDescriptor.Release();
                    return true;
                }
            }

            return false;
        }
        
        public bool MoveEntityToPlace(string entityId, string placeAddress, string checkpointId)
        {
            if (TryFindEntity(entityId, out var entityDescriptor))
            {
                if (placeAddress != entityDescriptor.place.address)
                {
                    entityDescriptor.Release();
                    var nextPlace = GetOrCreatePlace(placeAddress);
                    var nextData = entityDescriptor.data;
                    nextData.checkpointId = checkpointId;
                    nextData.gameAreaId = null;
                    nextPlace.CreateEntity(nextData);
                    return true;
                }
            }

            return false;
        }
        
        public bool MoveEntityToPlace(string entityId, string placeAddress, Vector3 position)
        {
            if (TryFindEntity(entityId, out var entityDescriptor))
            {
                if (placeAddress != entityDescriptor.place.address)
                {
                    entityDescriptor.Release();
                    var nextPlace = GetOrCreatePlace(placeAddress);
                    var nextData = entityDescriptor.data;
                    nextData.position = position;
                    nextData.checkpointId = null;
                    nextData.gameAreaId = null;
                    nextPlace.CreateEntity(nextData);
                    return true;
                }
            }

            return false;
        }

        public object SaveToWorld()
        {
            /* Save services data */
            var servicesData = new List<ServiceSaveData>();
            /* Walk services and get its data */
            foreach (var service in m_Services.Values)
            {
                if (service is IWorldSavable savable)
                {
                    var data = savable.SaveToWorld();
                    if (data != null)
                    {
                        servicesData.Add(new ServiceSaveData
                        {
                            serviceType = service.serviceType, // TODO: Проблема обратного преобразования, если сервис переименуется
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
                placesData[j] = (VirtualPlace.SaveData)(place.SaveToWorld());
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

        public async UniTask LoadFromWorld(object data)
        {
            if (data is SaveData saveData)
            {
                /* Load services */
                var tasks = new List<UniTask>();
                foreach (var serviceData in saveData.services)
                {
                    if (Service.Services.TryGetValue(serviceData.serviceType, out var service))
                    {
                        if (service is IWorldSavable savable)
                        {
                            tasks.Add(savable.LoadFromWorld(serviceData.data));
                        }
                    }
                    else
                    {
                        Console.Error("Launcher", $"Service not found: {serviceData.serviceType}");
                        GetOrCreateVirtualService(serviceData.serviceType).LoadFromWorld(serviceData.data);
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
                        await virtualPlace.TryAwake();
                    }
                    else
                    {
                        virtualPlace.TryHibernate();
                    }
                    loadTasks.Add(virtualPlace.LoadFromWorld(place));
                }
            
                await UniTask.WhenAll(loadTasks);
            }
        }

        public void Dispose()
        {
            if (m_Disposed)
            {
                Console.Error("VirtualWorld", "Dispose", "Already disposed");
                return;
            }

            m_Disposed = true;
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