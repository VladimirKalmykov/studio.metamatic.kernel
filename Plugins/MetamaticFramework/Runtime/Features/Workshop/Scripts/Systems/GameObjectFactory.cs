using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Metamatic;
using Metamatic.Framework.Workshop;
using Metamatic.Systems;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Metamatic.Framework
{
    public static class GameObjectFactory
    {
        private static readonly Dictionary<string, AsyncDisposableContainer<ConfigurablePool<UnityEngine.Component>>> Pools = new();
        private static readonly SignedIndexedPool<Action> SummonCancellationSignals = new(1000);
        
        public static async UniTask<T> SpawnFromBlueprint<T>(AssetReferenceT<ScriptableBlueprint<T>> assetReference, Signal cancellationSignal) where T: Component
        {
            return await SpawnFromBlueprint<T>((AssetReference)assetReference, cancellationSignal);
        }
        
        public static async UniTask<T> SpawnFromBlueprint<T>(AssetReferenceT<ScriptableBlueprint> assetReference, Signal cancellationSignal) where T: Component
        {
            return await SpawnFromBlueprint<T>((AssetReference)assetReference, cancellationSignal);
        }
        
        public static async UniTask<T> SpawnFromBlueprint<T>(AssetReference assetReference, Signal cancellationSignal) where T: Component
        {
            var poolContainer = GetOrAddPoolContainer(assetReference);
            var pool = await poolContainer.GetContent(cancellationSignal);
            var instanceSummon = pool.Summon();
            var instance = instanceSummon.resource;
            var recyclable = instance.gameObject.GetOrAddComponent<RecyclableBehaviour>();
            recyclable.OnRecycle += instanceSummon.Dispose;

            return (T)instance;
        }
        
        public static AsyncSummon<T> SummonFromBlueprint<T>(AssetReferenceT<ScriptableBlueprint<T>> assetReference) where T: Component
        {
            return SummonFromBlueprint<T>((AssetReference)assetReference);
        }
        
        public static AsyncSummon<T> SummonFromBlueprint<T>(AssetReferenceT<ScriptableBlueprint> assetReference) where T: Component
        {
            return SummonFromBlueprint<T>((AssetReference)assetReference);
        }
        
        public static AsyncSummon<T> SummonFromBlueprint<T>(AssetReference assetReference) where T: Component
        {
            var cancellationSignalSource = SignalSource.Create();
            var cancellationSignal = cancellationSignalSource.Signal;
            var token = SummonCancellationSignals.Add(cancellationSignalSource.TryRaise);
            var poolContainer = GetOrAddPoolContainer(assetReference);
            var task = poolContainer.GetContent(cancellationSignal).ContinueWith<ConfigurablePool<Component>, T>(pool =>
            {
                var summon = pool.Summon();
                cancellationSignal.Register(summon.Dispose);
                return (T)summon.resource;
            });
            return new AsyncSummon<T>(token, task, CancelSummon);
        }
        
        public static UniTask<T> SpawnFromPrefab<T>(AssetReferenceT<T> assetReference, Signal cancellationSignal) where T: Component
        {
            return SpawnFromPrefab<T>((AssetReference)assetReference, cancellationSignal);
        }
        
        public static AsyncSummon<T> SummonFromPrefab<T>(AssetReferenceT<T> assetReference) where T: Component
        {
            return SummonFromPrefab<T>((AssetReference)assetReference);
        }
        
        public static AsyncSummon<T> SummonFromPrefab<T>(AssetReference assetReference) where T: Component
        {
            var cancellationSignalSource = SignalSource.Create();
            var cancellationSignal = cancellationSignalSource.Signal;
            var token = SummonCancellationSignals.Add(cancellationSignalSource.TryRaise);
            var poolContainer = GetOrAddPoolContainer(assetReference);
            var task = poolContainer.GetContent(cancellationSignal).ContinueWith<ConfigurablePool<Component>, T>(pool =>
            {
                var summon = pool.Summon();
                cancellationSignal.Register(summon.Dispose);
                return (T)summon.resource;
            });
            return new AsyncSummon<T>(token, task, CancelSummon);
        }
        
        public static async UniTask<T> SpawnFromPrefab<T>(AssetReference assetReference, Signal cancellationSignal) where T: Component
        {
            var poolContainer = GetOrAddPoolContainer(assetReference);
            var pool = await poolContainer.GetContent(cancellationSignal);
            var instanceSummon = pool.Summon();
            var instance = instanceSummon.resource;
            var recyclable = instance.gameObject.GetOrAddComponent<RecyclableBehaviour>();
            recyclable.OnRecycle += instanceSummon.Dispose;

            return (T)instance;
        } 
        
        /// <summary>
        /// Increases the capacity of the pool by the specified amount. As the result of this operation, the pool will have at least the specified capacity available (spawned, but disabled).
        /// You can call this method multiple times to increase the capacity of the pool by the specified amount, but you must call <see cref="IDisposable.Dispose"/> on the returned object to decrease the capacity of the pool.
        /// </summary>
        /// <param name="assetReference"></param>
        /// <param name="capacity"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static IDisposable ReservePoolCapacity(AssetReference assetReference, int capacity)
        {
            var cts = SignalSource.Create();
            var typedContainer = GetOrAddPoolContainer(assetReference);
            #if DEBUG
            if (!typedContainer.isAlive)
            {
                throw new Exception("Failed to get container.");
            }
            #endif
            ConfigurablePool<UnityEngine.Component> resolvedPool = null;
            typedContainer.GetContent(cts.Signal).ContinueWith(pool =>
            {
                try
                {
                    resolvedPool = pool;
                    resolvedPool?.AddCapacity(capacity);
                } catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }).Forget();

            return DisposableAction.Create(() =>
            {
                resolvedPool?.WithdrawCapacity(capacity);
               cts.TryRaise();
            });
        }
        
        #region Internal private logic
        
        private static void CancelSummon(Token96Bit token)
        {
            if (SummonCancellationSignals.Remove(token, out var action))
            {
                action();
            }
        }
        
        private static AsyncDisposableContainer<ConfigurablePool<UnityEngine.Component>> GetOrAddPoolContainer(AssetReference assetReference)
        {
            if (!Pools.TryGetValue(assetReference.AssetGUID, out var poolContainer))
            {
                var newContainer = AsyncDisposableContainer<ConfigurablePool<UnityEngine.Component>>.Create(InitializePoolForAsset, assetReference);
                Pools.Add(assetReference.AssetGUID, newContainer);
                return newContainer;
            }
            return poolContainer;
        }

        private static async UniTask InitializePoolForAsset(Action<ConfigurablePool<UnityEngine.Component>> resolve, AssetReference assetReference, Signal cancellationSignal)
        {
            using var directResourceResourceSummon = ResourcesSystem.Summon(assetReference);
            var directResource = await directResourceResourceSummon.Wait(cancellationSignal);
            var assetReferenceRuntimeKey = assetReference.AssetGUID;
            switch (directResource)
            {
                case UnityEngine.Component component:
                    await InitializePoolForComponent(ResolveHandler, assetReferenceRuntimeKey, component, cancellationSignal);
                    break;
                case ScriptableBlueprint blueprint:
                    await InitializePoolForBlueprint(ResolveHandler, assetReferenceRuntimeKey, blueprint, cancellationSignal);
                    break;
                default:
                    throw new Exception($"Failed to initialize pool for asset {assetReference}");
            }

            return;

            void PoolEmptyHandler()
            {
                ReleasePoolContainer(assetReferenceRuntimeKey);
            }
            
            void ResolveHandler(ConfigurablePool<Component> pool)
            {
                pool.OnPoolIsUseless += PoolEmptyHandler;
                resolve(pool);
            }
        }
        
        private static async UniTask InitializePoolForComponent(Action<ConfigurablePool<UnityEngine.Component>> resolve, string runtimeKey, UnityEngine.Component component, Signal cancellationSignal)
        {
            var pool = new ConfigurablePool<UnityEngine.Component>(new ComponentPrefabPoolConfig<UnityEngine.Component>(component), autoShrink: true);
            resolve(pool);
            await cancellationSignal;
            pool.Dispose();
        }
        
        private static async UniTask InitializePoolForBlueprint(Action<ConfigurablePool<UnityEngine.Component>> resolve, string runtimeKey, ScriptableBlueprint blueprint, Signal cancellationSignal)
        {
            using var prefabSummon = blueprint.SummonPrefab(cancellationSignal);
            var prefab = await prefabSummon.Wait(cancellationSignal);
            var pool = new ConfigurablePool<UnityEngine.Component>(new ComponentPrefabPoolConfig<UnityEngine.Component>((UnityEngine.Component)prefab), autoShrink: true);
            resolve(pool);
            await cancellationSignal;
            pool.Dispose();
        }
        
        private static void ReleasePoolContainer(string runtimeKey)
        {
            if (Pools.TryGetValue(runtimeKey, out var poolContainer))
            {
                poolContainer.Dispose();
                Pools.Remove(runtimeKey);
            }
        }
        #endregion
    }
}