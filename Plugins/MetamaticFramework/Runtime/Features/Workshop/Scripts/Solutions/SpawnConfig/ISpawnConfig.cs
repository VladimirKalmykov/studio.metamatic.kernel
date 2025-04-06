using System.Collections.Generic;
using UnityEngine.AddressableAssets;

namespace Metamatic.Framework.Workshop
{
    public interface ISpawnConfig
    {
        public string configId { get; }
        public bool isValid { get; }
        public IEnumerable<Spawnpoint> spawnPoints { get; }
        public AssetReferenceT<EntityBlueprint> blueprint { get; }
        public bool TryGetSpawnPoint(string spawnpointId, out Spawnpoint spawnpoint);
    }
}