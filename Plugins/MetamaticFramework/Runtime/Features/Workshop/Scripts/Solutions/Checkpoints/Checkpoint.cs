
using System;
using Cysharp.Threading.Tasks;
using Metamatic.Exceptions;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Metamatic.Framework.Workshop
{
    using UnityEngine;

    public class Checkpoint : MonoBehaviour
    {
        private static readonly SignedIndexedHashPool<Checkpoint> s_Checkpoints = new SignedIndexedHashPool<Checkpoint>(50);
        private static event Action<Checkpoint, Entity> OnTriggerAll;
        public static DisposableAction SubscribeTriggerAll(Action<Checkpoint, Entity> action)
        {
            OnTriggerAll += action;
            return new DisposableAction(() => { OnTriggerAll -= action; });
        }
        public static bool TryGetCheckpoint(string id, out Checkpoint checkpoint)
        {
            /* Find all checkpoint components in the game */
            var checkpoints = FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);
            foreach (var nextCheckpoint in checkpoints)
            {
                if (nextCheckpoint.id == id)
                {
                    checkpoint = nextCheckpoint;
                    return true;
                }
            }
            checkpoint = null;
            return false;
        }
        
        public static bool TryGetCheckpointOrDefault(string id, out Checkpoint checkpoint)
        {
            /* Find all checkpoint components in the game */
            var checkpoints = FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);
            var defaultCheckpoint = default(Checkpoint);
            var gotDefault = false;
            foreach (var nextCheckpoint in checkpoints)
            {
                if (nextCheckpoint.id == id)
                {
                    checkpoint = nextCheckpoint;
                    return true;
                } else if (nextCheckpoint.m_IsDefault)
                {
                    defaultCheckpoint = nextCheckpoint;
                    gotDefault = true;
                }
            }
            checkpoint = defaultCheckpoint;
            return gotDefault;
        }
        
        public static bool TryGetDefaultCheckpoint(out Checkpoint checkpoint)
        {
            /* Find all checkpoint components in the game */
            var checkpoints = FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);
            foreach (var nextCheckpoint in checkpoints)
            {
                if (nextCheckpoint.m_IsDefault)
                {
                    checkpoint = nextCheckpoint;
                    return true;
                }
            }
            checkpoint = null;
            return false;
        }
        
        #if UNITY_EDITOR
        [FormerlySerializedAs("m_EditMode")] public Observable<bool> EditorOnlyEditMode = new(false); 
        #endif
        
        #region Serializable fields
        [SerializeField] public Vector3 spawnPosition;
        [Tooltip("Is this checkpoint the default one? New game will start from this checkpoint.")]
        [SerializeField] private bool m_IsDefault;
        
        [SerializeField]
        public string id;
        
        #endregion

        public event Action<Entity> OnTrigger;
        
        public Vector3 worldSpawnPosition => transform.TransformPoint(spawnPosition);
        private Observable<bool> m_ObservableIsActivated = new(false);
        private Action OnDestroyAction;
        private CheckpointTrigger m_CheckpointTrigger;
        private Token96Bit m_ReleaseToken;
        private bool m_GameAreaResolved;
        [ShowInInspector, ReadOnly]
        public GameArea m_GameArea { get; private set; }

        public GameArea gameArea
        {
            get
            {
                if (!m_GameAreaResolved)
                {
                    m_GameArea = transform.parent.GetComponent<GameArea>();
                    m_GameAreaResolved = true;
                }

                return m_GameArea;
            }
        }

        private void Awake()
        {
            m_ReleaseToken = s_Checkpoints.Add(this);
            m_CheckpointTrigger = GetComponent<CheckpointTrigger>();
            
            #if DEBUG
            if (transform.parent == null || gameArea == null)
            {
                GameSystem.FatalError("Checkpoint must be a child of GameArea");
            }
            #endif
            
            OnDestroyAction += gameArea.WatchActivatedChange(WatchAreaActivatedChange);
        }

        private void OnDestroy()
        {
            s_Checkpoints.Remove(m_ReleaseToken);
            OnDestroyAction?.Invoke();
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
            m_ObservableIsActivated.Value = isActivated;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        public void Trigger(Entity entity)
        {
            OnTrigger?.Invoke(entity);
            OnTriggerAll?.Invoke(this, entity);
        }
        
        public DisposableAction SubscribeTrigger(Action<Entity> action)
        {
            OnTrigger += action;
            return new DisposableAction(() => { OnTrigger -= action; });
        }
        
        /// <summary>
        /// Watch for activated change. Checkpoint is activated when the area is activated which contains this checkpoint.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public DisposableAction WatchActivationAsync(Func<bool, Signal, UniTaskVoid> action)
        {
            return m_ObservableIsActivated.WatchAsync(action);
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