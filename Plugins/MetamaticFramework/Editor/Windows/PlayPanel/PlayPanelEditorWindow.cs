using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Metamatic.EditorTools;
using Metamatic.Framework.Workshop;
using Metamatic.UIToolKit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;

namespace Metamatic.Framework.Editor.PlayPanel
{
    public class PlayPanelEditorWindow : AsyncEditorWindow
    {
        private const string BACKUP_SCENES_KEY = "Metamatic_CustomPlayMode_BackupScenes";
        
        #region Config
        private Observable<bool> m_Observable_ConfigSkipAppIntro = new Observable<bool>();
        private Observable<bool> m_Observable_ConfigSkipMainMenuScene = new Observable<bool>();
        private Observable<AssetReference> m_Observable_ChapterReference = new Observable<AssetReference>();
        #endregion
        
        #region Refs to UI elements
        private Toggle m_ToggleSkipAppIntro;
        private Toggle m_ToggleSkipMainMenuScene;   
        private AssetReferenceField m_ChapterAssetReferenceField;
        #endregion
        
        [UnityEditor.MenuItem ("Metamatic/Framework/Play Panel")]
        public static void ShowWindow()
        {
            GetWindow(typeof(PlayPanelEditorWindow), false, "Metamatic Play Panel");
        }

        protected override async UniTaskVoid EnableAsync(Func<UniTask> ready, Signal signal)
        {
            // Load play config from developer persistent storage
            m_Observable_ConfigSkipAppIntro.Value = EditorPrefs.GetBool(GameSystem.ConfigSkipGameIntroKeyName, false);
            m_Observable_ConfigSkipMainMenuScene.Value = EditorPrefs.GetBool(GameSystem.ConfigSkipMainMenuOnStartKeyName, false);
            m_Observable_ChapterReference.Value = EditorAddressablesUtils.GetAssetReferenceByAddress(EditorPrefs.GetString(GameSystem.ConfigForceStartChapterGuid, ""));
            
            // Подписываемся на событие изменения состояния Play Mode
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            
            // Apply config to UI
            m_ToggleSkipAppIntro.SetValueWithoutNotify(m_Observable_ConfigSkipAppIntro.Value);
            m_ToggleSkipMainMenuScene.SetValueWithoutNotify(m_Observable_ConfigSkipMainMenuScene.Value);
            m_ChapterAssetReferenceField.SetValue(m_Observable_ChapterReference.Value);
            
            // Subscribe to config
            using var listenSkipGameIntro = m_Observable_ConfigSkipAppIntro.Watch(OnSkipGameIntroChanged, true);
            using var listenSkipMainMenuScene = m_Observable_ConfigSkipMainMenuScene.Watch(OnSkipMainMenuSceneChanged, true);
            using var listenChapterReference = m_Observable_ChapterReference.Watch(OnChapterReferenceChanged, true);
            await ready();
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnSkipGameIntroChanged(bool value)
        {
            EditorPrefs.SetBool(GameSystem.ConfigSkipGameIntroKeyName, value);
            m_ToggleSkipAppIntro.SetValueWithoutNotify(value);
        }
        
        private void OnSkipMainMenuSceneChanged(bool value)
        {
            EditorPrefs.SetBool(GameSystem.ConfigSkipMainMenuOnStartKeyName, value);
            m_ToggleSkipMainMenuScene.SetValueWithoutNotify(value);
        }
        
        private void OnChapterReferenceChanged(AssetReference ar)
        {
            var address = EditorAddressablesUtils.EditorOnlyGetAddressFromAssetReference(ar);
            EditorPrefs.SetString(GameSystem.ConfigForceStartChapterGuid, address);
            m_ChapterAssetReferenceField.SetValue(ar);
        }

        protected override void OnCreateGUI()
        {
            rootVisualElement.styleSheets.Add(UIToolKitCore.tailwindStyleSheet);
            rootVisualElement.styleSheets.Add(UIToolKitCore.styleSheet);

            rootVisualElement.Add(CreatePlayPanel());
        }

        private VisualElement CreatePlayPanel()
        {
            var playPanel = new VisualElement();
            playPanel.AddToClassList(UIToolKitCore.Tailwind.Padding6);
            playPanel.AddToClassList(UIToolKitCore.Tailwind.Column);

            // Play button container
            var playButtonContainer = new VisualElement();
            playButtonContainer.AddToClassList(UIToolKitCore.Tailwind.Row);
            
            // Play button
            var button = new Button(EnterPlayMode)
            {
                text = "Pl"
            };
            button.style.flexBasis = new StyleLength(32);
            button.style.height = new StyleLength(32);
            
            playButtonContainer.Add(button);
            
            // Place selector (Object selector)
            m_ChapterAssetReferenceField = new AssetReferenceField(typeof(Place))
            {
                
            };
            m_ChapterAssetReferenceField.OnValueChanged += ar =>
            {
                m_Observable_ChapterReference.Value = ar;
            };
            
            playButtonContainer.Add(m_ChapterAssetReferenceField);
            
            
            playPanel.Add(playButtonContainer);
            
            // Options
            var optionsContainer = new VisualElement();
            
            // Skip intro scene checkbox
            m_ToggleSkipAppIntro = new Toggle("Skip app intro")
            {
                value = m_Observable_ConfigSkipAppIntro.Value
            };
            m_ToggleSkipAppIntro.RegisterValueChangedCallback(evt =>
            {
                m_Observable_ConfigSkipAppIntro.uniqValue = evt.newValue;
            });
            
            // Skip menu scene checkbox
            m_ToggleSkipMainMenuScene = new Toggle("Skip menu")
            {
                value = m_Observable_ConfigSkipMainMenuScene.Value
            };
            #if METAMATIC_SKIP_GAME_INTRO
                m_ToggleSkipAppIntro.value = true;
                m_ToggleSkipAppIntro.SetEnabled(false);
            #endif
            m_ToggleSkipMainMenuScene.RegisterValueChangedCallback(evt =>
            {
                m_Observable_ConfigSkipMainMenuScene.uniqValue = evt.newValue;
            });
            
            optionsContainer.Add(m_ToggleSkipAppIntro);
            optionsContainer.Add(m_ToggleSkipMainMenuScene);
            
            playPanel.Add(optionsContainer);

            return playPanel;
        }

        private void EnterPlayMode()
        {
            Debug.Log("[Debug1] Enter play mode with idle scene");
            string scenePath = GetUnityIdleScenePath();
            if (string.IsNullOrEmpty(scenePath))
            {
                EditorUtility.DisplayDialog("Ошибка", "Невалидный путь к сцене!", "OK");
                return;
            }

            // Сохраняем конфигурацию сцен:
            // Основная (активная) сцена
            var baseScene = EditorSceneManager.GetActiveScene().path;
            // Дополнительные сцены
            var additiveScenes = new HashSet<string>();
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                // Если это не активная сцена и путь задан, сохраняем его
                if (scene.path != baseScene && !string.IsNullOrEmpty(scene.path))
                    additiveScenes.Add(scene.path);
            }
            
            var additiveScenesArray = additiveScenes.ToArray();
            var backupScenes = $"{baseScene};{string.Join(";", additiveScenesArray)}";
            SessionState.SetString(BACKUP_SCENES_KEY, backupScenes);

            // Если есть несохранённые изменения – предложим их сохранить
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                // Открываем выбранную сцену (она заменит текущий набор)
                EditorSceneManager.OpenScene(scenePath);
                // Запускаем Play Mode
                EditorApplication.EnterPlaymode();
            }
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            Debug.Log($"[Debug1]Playmode changed: ${state}");
            // После выхода из Play Mode (когда редактор перешёл в режим редактирования)
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                var decodedScenesStr = SessionState.GetString(BACKUP_SCENES_KEY, "");
                if (string.IsNullOrEmpty(decodedScenesStr))
                {
                    Debug.LogWarning("No backup scenes found");
                    return;
                }
                // Split scenes
                var scenes = decodedScenesStr.Split(';');
                if (scenes.Length == 0)
                {
                    Debug.LogWarning("No backup scenes found");
                    return;
                }
                // Основная сцена
                var mainScenePath = scenes[0];
                // Дополнительные сцены
                var additiveScenePaths = new List<string>();
                for (int i = 1; i < scenes.Length; i++)
                {
                    additiveScenePaths.Add(scenes[i]);
                }
                Debug.Log($"Base scene path: ${mainScenePath}");
                Debug.Log($"Additive scene paths: ${string.Join(", ", additiveScenePaths)}");
                
                // Clear backup scenes in session state
                SessionState.EraseString(BACKUP_SCENES_KEY);

                // Восстанавливаем основную сцену (в одиночном режиме)
                EditorSceneManager.OpenScene(mainScenePath);

                // Затем открываем дополнительные сцены в режиме Additive
                foreach (string path in additiveScenePaths)
                {
                    EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                }
            }
        }

        /// <summary>
        /// Select first scene path
        /// </summary>
        /// <returns></returns>
        private string GetUnityIdleScenePath()
        {
            var scenes = EditorBuildSettings.scenes;
            if (scenes.Length == 0)
            {
                return null;
            }
            else
            {
                foreach (var scene in scenes)
                {
                    if (scene.enabled)
                    {
                        return scene.path;
                    }
                }
            }
            
            return null;
        }
        
        private AssetReference ResolveAssetReference(string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }
            return new AssetReference(guid);
        }
    }
}