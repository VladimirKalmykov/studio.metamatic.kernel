using System;
using Cysharp.Threading.Tasks;
using Metamatic;
using Metamatic.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Metamatic.Framework
{
    public class GameObjectFactoryPreloaderService: Service
    {
        [SerializeField]
        SerializableDictionary<AssetReference, int> m_AssetsToPreload = new SerializableDictionary<AssetReference, int>();

        protected override async UniTask RunService(Ready ready, Signal signal)
        {
            IDisposable[] preloadedAssets = new IDisposable[m_AssetsToPreload.Count];
            for (int i = 0; i < m_AssetsToPreload.Count; i++)
            {
                var kvp = m_AssetsToPreload[i];
                var assetReference = kvp.Key;
                var count = kvp.Value;

                preloadedAssets[i] = GameObjectFactory.ReservePoolCapacity(assetReference, count);
            }
            await ready();
            foreach (var preloadedAsset in preloadedAssets) 
            {
                preloadedAsset?.Dispose();
            }
        }
    }
}