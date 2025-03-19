using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Metamatic.EditorTools;
using Metamatic.UIToolKit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic.Framework.Editor.PlayPanel
{
    public class PlayPanelEditorWindow : AsyncEditorWindow
    {
        private string m_EditorMainScenePath;
        private readonly List<string> m_EditorAdditiveScenePaths = new List<string>();
        
        [UnityEditor.MenuItem ("Metamatic/Framework/Play Panel")]
        public static void ShowWindow()
        {
            GetWindow(typeof(PlayPanelEditorWindow), false, "Metamatic Play Panel");
        }

        protected override UniTaskVoid EnableAsync(Func<UniTask> ready, Signal signal)
        {
            return new UniTaskVoid();
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

            // Play button
            var button = new Button(EnterPlayMode)
            {
                text = "Play"
            };
            playPanel.Add(button);

            return playPanel;
        }

        private void EnterPlayMode()
        {
            Debug.Log("Enter play mode with idle scene");
            string scenePath = GetUnityIdleScenePath();
            if (string.IsNullOrEmpty(scenePath))
            {
                EditorUtility.DisplayDialog("Ошибка", "Невалидный путь к сцене!", "OK");
                return;
            }

            // Сохраняем конфигурацию сцен:
            // Основная (активная) сцена
            m_EditorMainScenePath = EditorSceneManager.GetActiveScene().path;
            // Дополнительные сцены
            m_EditorAdditiveScenePaths.Clear();
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                // Если это не активная сцена и путь задан, сохраняем его
                if (scene.path != m_EditorMainScenePath && !string.IsNullOrEmpty(scene.path))
                    m_EditorAdditiveScenePaths.Add(scene.path);
            }

            // Подписываемся на событие изменения состояния Play Mode
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

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
            // После выхода из Play Mode (когда редактор перешёл в режим редактирования)
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                // Отписываемся, чтобы обработчик не сработал повторно
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

                // Восстанавливаем основную сцену (в одиночном режиме)
                EditorSceneManager.OpenScene(m_EditorMainScenePath);

                // Затем открываем дополнительные сцены в режиме Additive
                foreach (string path in m_EditorAdditiveScenePaths)
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
    }
}