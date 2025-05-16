using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Metamatic.Framework.Workshop;
using Metamatic.Systems;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Metamatic.Framework
{
    /// <summary>
    /// Универсальная фабрика addressable‑объектов.
    /// * Один пул GameObject на каждый address/AssetGUID
    /// * Любой компонент извлекается лёгкой обёрткой <see cref="SummonComponent{T}"/>
    /// * <see cref="ReservePoolCapacity"/> по‑прежнему работает только с адресом
    /// </summary>
    public static class GameObjectFactory
    {
        #region Fields

        private static readonly Dictionary<string, AsyncContainer<ConfigurablePool<GameObject>>> Pools = new();
        private static readonly SignedIndexedPool<Action> SummonCancellationSignals = new(1000);

        #endregion

        #region Public — GameObject‑уровень

        public static AsyncSummon<GameObject> SummonGameObject(AssetReferenceT<GameObject> assetReference) => SummonGameObject((AssetReference)assetReference);
        public static AsyncSummon<GameObject> SummonGameObject(AssetReference assetReference)              => SummonGameObject(assetReference.AssetGUID);
        public static AsyncSummon<GameObject> SummonGameObject(string address)
        {
            var cancellationSrc = SignalSource.Create();
            var token          = SummonCancellationSignals.Add(cancellationSrc.TryRaise);

            var poolContainer = GetOrAddPoolContainer(address);
            var task = poolContainer.ToUniTask(cancellationSrc.Signal)
                                     .ContinueWith<ConfigurablePool<GameObject>, GameObject>(pool =>
                                     {
                                         var summon = pool.Summon();
                                         cancellationSrc.Signal.Register(summon.Dispose);
                                         return summon.resource;
                                     });

            return new AsyncSummon<GameObject>(token, task, CancelSummon);
        }

        #endregion

        #region Public — Component уровень

        /// <summary>
        /// Получить конкретный компонент из addressable‑префаба.
        /// В качестве «транспорта» используется <see cref="GameObject"/>‑пул,
        /// а компонент кэшируется через <see cref="ComponentCache{T}"/>.
        /// </summary>
        public static AsyncSummon<T> SummonComponent<T>(AssetReference assetReference) where T : Component =>
            SummonComponent<T>(assetReference.AssetGUID);

        public static AsyncSummon<T> SummonComponent<T>(string address) where T : Component
        {
            var goSummon = SummonGameObject(address);
            return WrapComponentSummon<T>(goSummon);
        }

        // ----------------------- Prefab helpers -----------------------
        public static AsyncSummon<T> SummonFromPrefab<T>(AssetReferenceT<GameObject> assetReference) where T : Component =>
            SummonComponent<T>((AssetReference)assetReference);

        public static AsyncSummon<T> SummonFromPrefab<T>(string assetGuid) where T : Component =>
            SummonComponent<T>(assetGuid);

        // ----------------------- Blueprint helpers -------------------
        public static AsyncSummon<T> SummonFromBlueprint<T>(AssetReferenceT<ScriptableBlueprint> assetReference) where T : Component =>
            SummonComponent<T>((AssetReference)assetReference);

        public static AsyncSummon<T> SummonFromBlueprint<T>(AssetReferenceT<ScriptableBlueprint<T>> assetReference) where T : Component =>
            SummonComponent<T>((AssetReference)assetReference);

        public static AsyncSummon<T> SummonFromBlueprint<T>(string assetGuid) where T : Component =>
            SummonComponent<T>(assetGuid);

        #endregion

        #region Capacity API

        public static IDisposable ReservePoolCapacity(AssetReference assetReference, int capacity) =>
            InternalReserve(assetReference.AssetGUID, capacity);

        public static IDisposable ReservePoolCapacity(string address, int capacity) =>
            InternalReserve(address, capacity);

        #endregion

        #region Internal helpers

        private static AsyncSummon<T> WrapComponentSummon<T>(AsyncSummon<GameObject> goSummon) where T : Component
        {
            if (goSummon.isEmpty)
                return AsyncSummon<T>.Empty;

            var task = goSummon.Wait(default)
                                 .ContinueWith<T>(ComponentCache<T>.GetOrAdd);

            // dispose компонента = dispose исходного GO‑summon
            return new AsyncSummon<T>(Token96Bit.Empty, task, _ => goSummon.Dispose());
        }

        private static IDisposable InternalReserve(string address, int capacity)
        {
            var cts       = SignalSource.Create();
            var container = GetOrAddPoolContainer(address);

            ConfigurablePool<GameObject> resolvedPool = null;
            container.ToUniTask(cts.Signal).ContinueWith(pool =>
            {
                resolvedPool = pool;
                resolvedPool?.AddCapacity(capacity);
            }).Forget();

            return DisposableAction.Create(() =>
            {
                resolvedPool?.WithdrawCapacity(capacity);
                cts.TryRaise();
            });
        }

        private static void CancelSummon(Token96Bit token)
        {
            if (SummonCancellationSignals.Remove(token, out var action))
                action();
        }

        #endregion

        #region Pool bootstrap

        private static AsyncContainer<ConfigurablePool<GameObject>> GetOrAddPoolContainer(AssetReference assetReference) =>
            GetOrAddPoolContainer(assetReference.AssetGUID);

        private static AsyncContainer<ConfigurablePool<GameObject>> GetOrAddPoolContainer(string key)
        {
            if (!Pools.TryGetValue(key, out var container))
            {
                container = AsyncContainer<ConfigurablePool<GameObject>>.Create(InitializePoolForAsset, key);
                Pools.Add(key, container);
            }
            return container;
        }

        private static async UniTask InitializePoolForAsset(Action<ConfigurablePool<GameObject>> resolve, string address, Signal cancel)
        {
            using var summon = ResourcesSystem.Summon<UnityEngine.Object>(address);
            var directRes    = await summon.Wait(cancel);

            GameObject prefab = directRes switch
            {
                GameObject go           => go,
                Component comp          => comp.gameObject,
                ScriptableBlueprint bp  => await bp.SummonPrefab().Wait(cancel),
                _                       => throw new Exception($"Unsupported asset type for {address}")
            };

            var pool = new ConfigurablePool<GameObject>(
                new GameObjectConfigurablePoolConfig(prefab, keepInactive: true, dontDestroyOnLoad: true),
                autoShrink: true);

            pool.OnPoolIsUseless += () => ReleasePool(address);

            resolve(pool);

            await cancel;
            pool.Dispose();
        }

        private static void ReleasePool(string key)
        {
            if (Pools.TryGetValue(key, out var container))
            {
                container.Dispose();
                Pools.Remove(key);
            }
        }

        #endregion
    }
}
