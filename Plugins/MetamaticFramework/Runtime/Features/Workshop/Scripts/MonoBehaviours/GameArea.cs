#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Sirenix.OdinInspector;

namespace Metamatic.Framework.Workshop
{
    public class GameArea : MonoBehaviour
    {
        #if UNITY_EDITOR
        [SerializeField][HideInInspector] public bool isActiveInEditor;
        #endif
        
        [ShowInInspector, ReadOnly]
        private bool m_FactualIsActiveInEditor = false;
        
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

        private void OnEnable()
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying && isActiveInEditor)
            {
                EditorOnlyActivate();
            }
            #endif
        }
        
        private void OnDisable()
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying && isActiveInEditor)
            {
                EditorOnlyDeactivate();
            }
            #endif
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
    }
}