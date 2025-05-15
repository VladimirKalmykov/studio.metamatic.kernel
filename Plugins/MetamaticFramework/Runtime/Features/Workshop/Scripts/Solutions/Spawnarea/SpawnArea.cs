using System;
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
    public abstract class SpawnArea: MonoBehaviour
    {
        #region Static
        private static SignedIndexedHashPool<SpawnArea>
            m_SpawnAreas = new SignedIndexedHashPool<SpawnArea>(100);
        
        public static DisposableAction WatchSpawnpointAsync(Func<SpawnArea, Signal, UniTaskVoid> onSpawnpointChanged, Func<SpawnArea, bool> filter = null)
        {
            return m_SpawnAreas.WatchEveryAsync(onSpawnpointChanged, filter);
        }
        
        public static UniTask<SpawnArea> WaitForSpawnpoint(Func<SpawnArea, bool> filter, Signal cancellationSignal)
        {
            Action onDispose = null;
            var source = SafeUniTaskCompletionSource<SpawnArea>.Create();
            
            var disposeWatch = m_SpawnAreas.WatchEvery(OnAdded);
            onDispose += cancellationSignal.Register(source.TrySetCanceledAndForget);
            onDispose += cancellationSignal.Register(disposeWatch);
            
            void OnAdded(SpawnArea spawnArea)
            {
                if (filter(spawnArea))
                {
                    onDispose?.Invoke();
                    source.TrySetResult(spawnArea);
                }
            }
            
            return source.Task;
        }
        #endregion
        
        [Tooltip("Unique identifier of the spawn area")]
        [ReadOnly] public string id;
        [SerializeField] public Color devToolsColor = new Color(0.05f, 0.37f, 0.73f);
        
        #region Abstract Methods

        public abstract bool TryGetSpawnpoint(out Spawnpoint spawnpoint);
        public abstract IEnumerator<Spawnpoint> GetEnumerator();
        public abstract bool TryGetSpawnPoint(string spawnpointId, out Spawnpoint spawnpoint);
        private Token96Bit m_RegistrationToken;
        #endregion
        
        
        
        #region Mono

        private void OnEnable()
        {
            m_RegistrationToken = m_SpawnAreas.Add(this);
        }

        private void OnDisable()
        {
            m_SpawnAreas.Remove(m_RegistrationToken);
        }
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