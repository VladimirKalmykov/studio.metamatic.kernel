#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Metamatic.Systems;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Sirenix.OdinInspector;
using UnityEngine.SceneManagement;

namespace Metamatic.Framework.Workshop
{
    public abstract class GameArea : MonoBehaviour
    {
        public static readonly List<GameArea> AllAreas = new List<GameArea>();
        public static readonly List<GameArea> ActiveAreas = new List<GameArea>();
        /// <summary>
        /// Determines if the position is intersected with any active areas.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static bool IsIntersectedWithActiveAreas(Vector3 position)
        {
            foreach (var area in ActiveAreas)
            {
                if (area.IsIntersected(position))
                {
                    return true;
                }
            }

            return false;
        }
        
        /// <summary>
        /// Finds intersected area by position.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="includingInactive"></param>
        /// <param name="area"></param>
        /// <returns></returns>
        public static bool TryFindIntersectArea(Vector3 position, bool includingInactive, out GameArea area)
        {
            foreach (var activeArea in ActiveAreas)
            {
                if ((includingInactive || activeArea.isActive) && activeArea.IsIntersected(position))
                {
                    area = activeArea;
                    return true;
                }
            }

            area = null;
            return false;
        }
        
        public static bool TryGetAreaById(string id, out GameArea area)
        {
            foreach (var nextArea in AllAreas)
            {
                if (nextArea.id == id)
                {
                    area = nextArea;
                    return true;
                }
            }

            area = null;
            return false;
        }
        
#if UNITY_EDITOR
        [SerializeField] [HideInInspector] public bool isActiveInEditor;
#endif
        [Tooltip("Determines that all entities that belong to this area should be removed or virtualized if the area is deactivated. To put it simply, all things will fall down to the abyss if this area's basement are disappeared.")]
        [SerializeField] public bool isFoundation = true;
        [ReadOnly] [SerializeField] public string id;

#if UNITY_EDITOR        
        private bool m_FactualIsActiveInEditor = false;
#endif

        public bool isActive { get; private set; } = false;

        public enum ResourceMode
        {
            Prefab,
            AddictiveScene
        }

        [SerializeField] private ResourceMode m_ResourceMode = ResourceMode.Prefab;

        [ShowIf("@m_ResourceMode==ResourceMode.Prefab")] [SerializeField]
        private AssetReferenceT<Transform> m_Prefab;

        [ShowIf("@m_ResourceMode==ResourceMode.AddictiveScene")] [SerializeField]
        private AssetReference m_AddictiveScene;

#if UNITY_EDITOR
        // Храним ссылки на созданные объекты/сцены для последующей деактивации
        private GameObject m_InstantiatedPrefab;
        private UnityEngine.SceneManagement.Scene m_LoadedScene;
#endif
        
        public event Action<GameArea> OnActivateEvent;
        public event Action<GameArea> OnBeforeDeactivate;

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && isActiveInEditor)
            {
                EditorOnlyActivate();
            }
#endif
            GameAreas2dService.RegisterGameArea(this);
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && isActiveInEditor)
            {
                EditorOnlyDeactivate();
            }
#endif
            GameAreas2dService.UnregisterGameArea(this);
        }

        private void GenerateUniqueId(bool force = false)
        {
            id = Guid.NewGuid().ToString();
        }

#if UNITY_EDITOR
        public void EditorOnlyApplyActivation()
        {
            if (Application.isPlaying) return;
            if (isActiveInEditor)
            {
                EditorOnlyActivate();
            }
            else
            {
                EditorOnlyDeactivate();
            }
        }
#endif

        /// <summary>
        /// Активирует область:
        /// - Если режим Prefab, то загружает префаб и добавляет его в сцену.
        /// - Если режим AddictiveScene, то открывает сцену в режиме Additive.
        /// Работает только в редакторе.
        /// </summary>
        public void EditorOnlyActivate()
        {
#if UNITY_EDITOR
            if (m_FactualIsActiveInEditor) return;
            m_FactualIsActiveInEditor = true;
            if (m_ResourceMode == ResourceMode.Prefab)
            {
                // Получаем путь к ассету через его GUID
                string assetPath = AssetDatabase.GUIDToAssetPath(m_Prefab.AssetGUID);
                if (string.IsNullOrEmpty(assetPath))
                {
                    Debug.LogError("Путь к префабу не найден");
                    return;
                }

                // Загружаем ассет
                GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefabAsset == null)
                {
                    Debug.LogError("Не удалось загрузить префаб по пути: " + assetPath);
                    return;
                }

                // Инстанцируем префаб в сцене через PrefabUtility (для сохранения связи с оригиналом)
                m_InstantiatedPrefab = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
                if (m_InstantiatedPrefab != null)
                {
                    Undo.RegisterCreatedObjectUndo(m_InstantiatedPrefab, "Activate GameArea Prefab");
                }
            }
            else if (m_ResourceMode == ResourceMode.AddictiveScene)
            {
                // Получаем путь к сцене через её GUID
                string scenePath = AssetDatabase.GUIDToAssetPath(m_AddictiveScene.AssetGUID);
                if (string.IsNullOrEmpty(scenePath))
                {
                    Debug.LogError("Путь к сцене не найден");
                    return;
                }

                // Открываем сцену в режиме Additive
                m_LoadedScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            }
#else
            Debug.LogWarning("Activate работает только в режиме редактора");
#endif
        }

        /// <summary>
        /// Деактивирует область:
        /// - Если режим Prefab, то удаляет ранее созданный экземпляр.
        /// - Если режим AddictiveScene, то пытается закрыть добавленную сцену, предварительно проверив, нет ли несохранённых изменений.
        /// Работает только в режиме редактора.
        /// </summary>
        public void EditorOnlyDeactivate()
        {
#if UNITY_EDITOR
            if (!m_FactualIsActiveInEditor) return;
            m_FactualIsActiveInEditor = false;
            if (m_ResourceMode == ResourceMode.Prefab)
            {
                if (m_InstantiatedPrefab != null)
                {
                    // Удаляем созданный объект с регистрацией Undo
                    Undo.DestroyObjectImmediate(m_InstantiatedPrefab);
                    m_InstantiatedPrefab = null;
                }
            }
            else if (m_ResourceMode == ResourceMode.AddictiveScene)
            {
                if (m_LoadedScene.IsValid())
                {
                    // Если сцена имеет несохранённые изменения, запрещаем деактивацию
                    if (m_LoadedScene.isDirty)
                    {
                        EditorUtility.DisplayDialog("Невозможно деактивировать",
                            "В добавленной сцене есть несохранённые изменения. Сохраните сцену перед деактивацией.",
                            "ОК");
                        Debug.LogWarning("Деактивация прервана – изменения в сцене не сохранены");
                        return;
                    }

                    // Закрываем сцену (forceUnload = true)
                    EditorSceneManager.CloseScene(m_LoadedScene, true);
                }
            }
#else
            Debug.LogWarning("Deactivate работает только в режиме редактора");
#endif
        }

        #region Mono

        private void Awake()
        {
            AllAreas.Add(this);
        }

        private void OnDestroy()
        {
            AllAreas.Remove(this);
        }

        #endregion
        
        public async UniTask Activate(Signal cancellationSignal)
        {
            if (isActive)
            {
                Console.Exception("GameArea", "Already active");
                return;
            }

            isActive = true;
            ActiveAreas.Add(this);

            try
            {
                if (m_ResourceMode == ResourceMode.Prefab)
                {
                    if (m_Prefab == null)
                    {
                        Console.Exception("GameArea", "Prefab is not set");
                        return;
                    }

                    await ActivatePrefab(cancellationSignal);
                }
                else if (m_ResourceMode == ResourceMode.AddictiveScene)
                {
                    if (m_AddictiveScene == null)
                    {
                        Console.Exception("GameArea", "Scene is not set");
                        return;
                    }

                    await ActivateScene(cancellationSignal);
                }
            }
            catch (OperationCanceledException)
            {
                // Skip
            }
            catch (Exception e)
            {
                Console.Exception("GameArea", e);
            }
            finally
            {
                isActive = false;
                ActiveAreas.Remove(this);
            }
        }
        
        private async UniTask ActivatePrefab(Signal cancellationSignal)
        {
            /* Load prefab resource */
            using var prefabSummon = ResourcesSystem.Summon<Transform>(m_Prefab);
            var prefab = await prefabSummon.Wait(cancellationSignal);
            /* Create instance */
            var thisTransform = transform;
            var instance = Instantiate(prefab, thisTransform);
            OnActivateEvent?.Invoke(this);
            await cancellationSignal;
            OnBeforeDeactivate?.Invoke(this);
            Destroy(instance.gameObject);
        }
        
        private async UniTask ActivateScene(Signal cancellationSignal)
        {
            /* Load scene */
            await using var sceneSummon = ResourcesSystem.LoadScene(m_AddictiveScene, LoadSceneMode.Additive);
            await sceneSummon.Wait(cancellationSignal);
            OnActivateEvent?.Invoke(this);
            await cancellationSignal;
            OnBeforeDeactivate?.Invoke(this);
        }
        
        public DisposableAction SubscribeBeforeDeactivate(Action<GameArea> handler)
        {
            OnBeforeDeactivate += handler;
            return new DisposableAction(() => OnBeforeDeactivate -= handler);
        }
        
        public DisposableAction SubscribeActivated(Action<GameArea> handler)
        {
            OnActivateEvent += handler;
            return new DisposableAction(() => OnActivateEvent -= handler);
        }
        
        #region Abstract methods

        public abstract bool IsIntersected(Vector3 position);
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
            }
            else if (string.IsNullOrEmpty(id))
            {
                GenerateUniqueId();
                EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}