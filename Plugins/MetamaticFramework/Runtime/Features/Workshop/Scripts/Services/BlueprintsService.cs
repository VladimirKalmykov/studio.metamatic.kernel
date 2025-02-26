using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Metamatic.Systems;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Metamatic.Framework.Workshop
{
    public class BlueprintsService: Service
    {
        protected override UniTask RunService(Ready ready, Signal signal)
        {
            throw new System.NotImplementedException();
        }

        public async UniTaskVoid CreateBlueprintPool(AssetReferenceT<ScriptableBlueprint> assetReference, Signal cancellationSignal = default)
        {
            var blueprintSummon = ResourcesSystem.Summon<ScriptableBlueprint>(assetReference);
            var blueprint = await blueprintSummon.Wait(cancellationSignal);
            var pool = new ConfigurablePool<GameObject>(new GameObjectConfigurablePoolConfig());
        }
    }
}