using System.Collections.Generic;

namespace Metamatic.Framework.Workshop
{
    using Cysharp.Threading.Tasks;
    using Metamatic;
    using Sirenix.OdinInspector;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    
    [RequireComponent(typeof(Entity))]
    public class AutoSpawnConfig : SpawnConfig
    {
        [SerializeField] private AssetReferenceT<EntityBlueprint>[] m_Blueprints;
        [SerializeField] public SpawnArea m_SpawnArea;

        [Tooltip("If true, it use IEnumerable of spawn area to spawn")] [SerializeField]
        private bool m_AutoCount = true;

        [HideIf("m_AutoCount")] [SerializeField]
        private int m_Count = 10;
        
        public AssetReference[] blueprints => m_Blueprints;
        public SpawnArea spawnArea => m_SpawnArea;
        public bool autoCount => m_AutoCount;
        public int count => m_Count;
        

        public override bool isValid
        {
            get
            {
                if (m_Blueprints == null || m_Blueprints.Length == 0)
                {
                    Debug.LogError("AutoSpawnConfig: No blueprints assigned");
                    return false;
                }
                if (m_SpawnArea == null)
                {
                    Debug.LogError("AutoSpawnConfig: No spawn area assigned");
                    return false;
                }
                if (m_AutoCount && m_Count <= 0)
                {
                    Debug.LogError("AutoSpawnConfig: Count must be greater than 0");
                    return false;
                }

                return true;
            }
        }

        public override IEnumerable<Spawnpoint> spawnPoints
        {
            get
            {
                if (m_SpawnArea != null)
                {
                    foreach (var spawnpoint in m_SpawnArea)
                    {
                        yield return spawnpoint;
                    }
                }
                else
                {
                    Debug.LogError("AutoSpawnConfig: No spawn area assigned");
                }
            }
        }

        public override AssetReferenceT<EntityBlueprint> blueprint
        {
            get
            {
                // Get random blueprint from array
                if (m_Blueprints != null && m_Blueprints.Length > 0)
                {
                    return m_Blueprints[Random.Range(0, m_Blueprints.Length)];
                }
                else
                {
                    Debug.LogError("AutoSpawnConfig: No blueprints assigned");
                    return null;
                }
            }
        }
        public override bool TryGetSpawnPoint(string spawnpointId, out Spawnpoint spawnpoint)
        {
            return m_SpawnArea.TryGetSpawnPoint(spawnpointId, out spawnpoint);
        }
    }
}