using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Serialization;

namespace Metamatic.Framework.Workshop
{
    [RequireService(typeof(SpawnService))]
    public abstract class SpawnArea: MonoBehaviour
    {
        [Tooltip("Unique identifier of the spawn area")]
        [ReadOnly] public string id;
        [SerializeField] public Color devToolsColor = new Color(0.05f, 0.37f, 0.73f);
        
        #region Abstract Methods

        public abstract bool TryGetSpawnpoint(out Spawnpoint spawnpoint);
        #endregion
        
#if UNITY_EDITOR
        // Метод вызывается при изменениях в редакторе
        protected void OnValidate()
        {
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
        
        private static bool IsMatchingId(SpawnArea other, string id)
        {
            return other.id == id;
        }
#endif
    }
}