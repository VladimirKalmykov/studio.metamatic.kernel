using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Metamatic;
using Metamatic.Framework.Workshop;
using Metamatic.SaveSystem;
using Metamatic.Systems;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace Metamatic.Framework.Workshop
{
    public class EntitiesService : Service, ISavable
    {
        [Serializable]
        [Version("1.0.0")]
        public struct SaveData
        {
            public Entity.SaveData[] entities;
        }
        
        private static readonly SignedIndexedPool<Entity> EntitiesRegistry = new(200);
        private static readonly Dictionary<string, Token96Bit> EntitiesRegistrationTokens = new();
        private static readonly Dictionary<string, Action<Entity, bool>> EntitiesRegistrationEventListeners = new();
        
        [ShowInInspector]
        public int entitiesCount => EntitiesRegistry.Count;
        
        /// <summary>
        /// Try to register entity in the registry. If entity already registered, returns empty token.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static Token96Bit TryRegisterEntity(Entity entity)
        {
            if (EntitiesRegistrationTokens.TryGetValue(entity.id, out var token))
            {
                return Token96Bit.Empty;
            }
            var registrationToken = EntitiesRegistry.Add(entity);
            var result = EntitiesRegistrationTokens[entity.id] = registrationToken;
            if (EntitiesRegistrationEventListeners.TryGetValue(entity.id, out var listener))
            {
                listener(entity, true);
            }
            return result;
        }
        
        /// <summary>
        /// Try to unregister entity from the registry. If entity not found, returns false.
        /// </summary>
        /// <returns></returns>
        public static bool TryUnregisterEntity(Token96Bit registrationToken)
        {
            if (EntitiesRegistry.Remove(registrationToken, out var entity))
            {
                if (EntitiesRegistrationEventListeners.TryGetValue(entity.id, out var listener))
                {
                    listener(entity, false);
                    EntitiesRegistrationEventListeners.Remove(entity.id);
                }
                return true;
            }

            return false;
        }
        
        public static bool TryGetEntityById(string id , out Entity result)
        {
            if (EntitiesRegistrationTokens.TryGetValue(id, out var token))
            {
                if (EntitiesRegistry.TryGet(token, out var entity))
                {
                    result = entity;
                    return true;
                }
            }

            result = null;
            return false;
        }
        
        public static DisposableAction WatchEntity(string id, Action<Entity, bool> listener)
        {
            if (EntitiesRegistrationEventListeners.TryGetValue(id, out var existingListener))
            {
                EntitiesRegistrationEventListeners[id] = existingListener + listener;
            }
            else
            {
                EntitiesRegistrationEventListeners[id] = listener;
            }
            EntitiesRegistrationEventListeners[id] = listener;
            return new DisposableAction(() =>
            {
                if (EntitiesRegistrationEventListeners.TryGetValue(id, out var postExistingListener))
                {
                    postExistingListener -= listener;
                    if (postExistingListener == null)
                    {
                        EntitiesRegistrationEventListeners.Remove(id);
                    }
                    else
                    {
                        EntitiesRegistrationEventListeners[id] = postExistingListener;
                    }
                }
            });
        }

        public static DisposableAction WatchEntityAsync(string id, Func<Entity, Signal, UniTaskVoid> handler)
        {
            SignalSource cancellationSignalSource = SignalSource.Create();
            var cancellationSignal = cancellationSignalSource.Signal;
            SignalSource releaseSignalSource = default;
            var listener = WatchEntity(id, (entity, registered) =>
            {
                if (registered)
                {
                    releaseSignalSource = SignalSource.Create();
                    var signal = releaseSignalSource.Signal;
                    handler(entity, signal).Forget();
                }
                else
                {
                    releaseSignalSource.TryRaise();
                }
            });
            return new DisposableAction(() =>
            {
                listener.Dispose();
                releaseSignalSource.TryRaise();
                cancellationSignalSource.TryRaise();
            });
        }
        
        public static UniTask<Entity> WaitForEntity(string id, Signal cancellationSignal)
        {
            if (EntitiesRegistrationTokens.TryGetValue(id, out var token))
            {
                if (EntitiesRegistry.TryGet(token, out var result))
                {
                    return UniTask.FromResult(result);
                }
            }
            var completionSource = SafeUniTaskCompletionSource<Entity>.Create();
            DisposableAction signalListener = default;
            var listener = WatchEntity(id, (entity, registered) =>
            {
                if (registered)
                {
                    signalListener.Dispose();
                    completionSource.TrySetResult(entity);
                }
                else
                {
                    Console.Error("EntitiesService", $"Unexpected behaviour. Entity with id {id} was unregistered before it was registered");
                }
            });
            signalListener = cancellationSignal.Register(() =>
            {
                completionSource.TrySetCanceled();
                listener.Dispose();
            });
            return completionSource.Task;
        }

        protected override UniTask RunService(Ready ready, Signal signal)
        {
            return ready();
        }

        /// <summary>
        /// Spawn entity by blueprint reference. You can provide an id, in this case the entity will be reused if it already exists on the scene (in this case you alike to say 'I need this entity to be here - on scene').
        /// </summary>
        /// <param name="blueprintRef"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="id"></param>
        /// <param name="activate"></param>
        /// <param name="cancellationSignal"></param>
        /// <returns></returns>
        public async UniTask<Entity> SpawnEntity(
            AssetReferenceT<EntityBlueprint> blueprintRef,
            Vector3 position = default,
            Quaternion rotation = default,
            string id = null,
            bool activate = false,
            Signal cancellationSignal = default
        )
        {
            Entity entity;

            if (id != null && TryGetEntityById(id, out entity))
            {
                // Set entity position and rotation
                var entityTransform = entity.transform;
                entityTransform.position = position;
                entityTransform.rotation = rotation;
            }
            else
            {
                AsyncSummon<Entity> summon = default;
                try
                {
                    // TODO: Here we can check if the pool for blueprint exists and if it does, we can spawn from the pool
                    
                    /* Summon blueprint */
                    summon= GameObjectFactory.SummonFromBlueprint<Entity>((AssetReference)blueprintRef);
                    var entityPrefab = await summon.Wait(cancellationSignal);

                    // Instantiate
                    entity = Instantiate(entityPrefab, position, rotation);
                    // if not from pool
                    var rb = entity.GetOrAddComponent<RecyclableBehaviour>();
                    cancellationSignal.ThrowIfRaised();
                    var registrationToken = TryRegisterEntity(entity);
                    // Release blueprint and prefab after entity is destroyed
                    rb.OnRecycle += () =>
                    {
                        TryUnregisterEntity(registrationToken);
                        
                        // We can dispose blueprint and prefab only after entity is destroyed
                        summon.Dispose();
                    };
                    // Add entity to the dictionary
                    
                }
                catch (OperationCanceledException e)
                {
                    summon.Dispose();
                    return null;
                }
            }

            if (activate)
            {
                entity.gameObject.SetActive(true);
            }

            return entity;
        }

        public object Save()
        {
            var entitiesData = new Entity.SaveData[EntitiesRegistry.Count];
            var saveData = new SaveData
            {
                entities = entitiesData
            };
            var i = 0;
            foreach (var pairs in EntitiesRegistrationTokens)
            {
                if (EntitiesRegistry.TryGet(pairs.Value, out var entity))
                {
                    entitiesData[i] = (Entity.SaveData)(entity.Save());
                    i++;
                } else {
                    Console.Error("EntitiesService", $"Unexpected behaviour. Entity with id {pairs.Key} was not found in the registry");
                }
            }

            return saveData;
        }

        public async UniTask Load(object data)
        {
            var spawnEntitiesIds = new HashSet<string>();
            var saveData = (SaveData)data;
            
            /* Collect entity ids */
            foreach (var entitySaveData in saveData.entities)
            {
                spawnEntitiesIds.Add(entitySaveData.id);
            }
            
            /* Farewell, old entities */
            foreach (var pairs in EntitiesRegistrationTokens)
            {
                if (EntitiesRegistry.TryGet(pairs.Value, out var entity))
                {
                    // Remove only those entities that are not in the save data
                    if (!spawnEntitiesIds.Contains(entity.id))
                    {
                        entity.GetOrAddComponent<RecyclableBehaviour>().Recycle();
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

        public UniTask Bootstrap()
        {
            // Do nothing
            return UniTask.CompletedTask;
        }

        private async UniTask LoadEntity(Entity.SaveData entitySaveData)
        {
            Entity entity = null;
            if (entitySaveData.isStatic)
            {
                if (EntitiesRegistrationTokens.TryGetValue(entitySaveData.id, out var token))
                {
                    if (EntitiesRegistry.TryGet(token, out entity))
                    {
                        var entityTransform = entity.transform;
                        entityTransform.position = entitySaveData.position;
                        entityTransform.rotation = entitySaveData.rotation;
                    }
                }

                if (entity == null)
                {
                    // Entity is static but not found in the dictionary, probably it was destroyed within the subscene
                    // TODO: Add entity to virtual scene
                    return;
                }
            }
            else
            {
                entity = await SpawnEntity(entitySaveData.blueprint, entitySaveData.position,
                    entitySaveData.rotation, id: entitySaveData.id, activate: false);
            }

            entity.ResetEntity();
            entity.Load(entitySaveData);
        }
    }
}