
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Metamatic.Framework.Workshop
{
    using UnityEngine;

    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] private Vector3 m_SpawnPosition;
        [Tooltip("Is this checkpoint the default one? New game will start from this checkpoint.")]
        [SerializeField] private bool m_IsDefault;

        [SerializeField]
        public string id;

        private Action OnDestrotAction;
        private CheckpointTrigger m_CheckpointTrigger;

        private void Awake()
        {
            var gameArea = GetComponentInParent<GameArea>();
            m_CheckpointTrigger = GetComponent<CheckpointTrigger>();
            OnDestrotAction += gameArea.WatchActivatedChange(WatchAreaActivatedChange);
        }

        private void OnDestroy()
        {
            OnDestrotAction?.Invoke();
        }

        private void GenerateUniqueId()
        {
            id = IdsGenerator.GenerateShortGuid();
        }

        private void WatchAreaActivatedChange(bool isActivated)
        {
            if (isActivated)
            {
                m_CheckpointTrigger.Activate(this);
            }
            else
            {
                m_CheckpointTrigger.Deactivate();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        public void OnTrigger(Entity entity)
        {
            
        }
        
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
                GenerateUniqueId();
                // Отмечаем объект как изменённый, чтобы изменения сохранились
                EditorUtility.SetDirty(this);
            }
            else
            {
                // Check others waypoints in the scene
                var checkpoints = FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);
                foreach (var checkpoint in checkpoints)
                {
                    if (checkpoint == this) continue;
                    if (checkpoint.id == id)
                    {
                        GenerateUniqueId();
                        EditorUtility.SetDirty(this);
                        break;
                    }
                }
            }
        }
#endif
    }

}