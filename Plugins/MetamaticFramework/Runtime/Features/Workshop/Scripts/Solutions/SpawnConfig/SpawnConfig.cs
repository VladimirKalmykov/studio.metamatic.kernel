using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Metamatic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Metamatic.Framework.Workshop
{
    public abstract class SpawnConfig: AsyncBehaviour, ISpawnConfig
    {
        public string id;
        public string configId => id;
        protected override async UniTask Run(Ready ready, Signal signal)
        {
            await signal;
        }

        #region Abstract
        public abstract bool isValid { get; }
        public abstract IEnumerable<Spawnpoint> spawnPoints { get; }
        public abstract AssetReferenceT<EntityBlueprint> blueprint { get; }
        public abstract bool TryGetSpawnPoint(string spawnpointId, out Spawnpoint spawnpoint);

        #endregion
        
#if UNITY_EDITOR
        // Метод вызывается при изменениях в редакторе
        protected override void OnValidate()
        {
            base.OnValidate();
            // EditorUtility.IsPersistent(gameObject)
            if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
            {
                /* Erase Id for prefab assets */
                id = null;
                EditorUtility.SetDirty(this);
            } else if (string.IsNullOrEmpty(id))
            {
                if (IdsGenerator.GenerateUniqueIdForComponent(this, IdsGenerator.IdType.ShortGuid, IsMatchingId, out var newId))
                {
                    id = newId;
                    EditorUtility.SetDirty(this);
                }
            }
        }
        
        private static bool IsMatchingId(SpawnConfig other, string id)
        {
            return other.id == id;
        }
#endif
    }
}