using Cysharp.Threading.Tasks;
using Metamatic;
using Metamatic.EditorTools;
using Metamatic.UIToolKit;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic.Framework.Workshop
{
    [CustomEditor(typeof(GameArea2D))]
    public class GameArea2DEditor : AsyncOdinEditor
    {
        public enum PreviewMode
        {
            Disabled,
            Enabled
        }

        private GameArea2D m_Area2D;
        private Observable<PreviewMode> m_PreviewMode = new(PreviewMode.Disabled);
        private Button m_TogglePreviewButton;

        protected override VisualElement CreateInspectorGUIAsync()
        {
            var superRoot = new VisualElement();
            superRoot.AddToClassList("Root");
            // Connect stylesheets
            superRoot.styleSheets.Add(UIToolKitCore.tailwindStyleSheet);
            superRoot.styleSheets.Add(UIToolKitCore.styleSheet);

            VisualElement ugui = new IMGUIContainer(() =>
            {
                if (target != null)
                {
                    OnInspectorGUI();
                }
            });

            superRoot.Add(ugui);
            superRoot.Add(CreateControlsElement());

            Console.Debug("Editor", "Create GUI");

            return superRoot;
        }

        private VisualElement CreateControlsElement()
        {
            m_TogglePreviewButton = new Button()
            {
                text = "Activate editor"
            };
            // Listen to click
            m_TogglePreviewButton.clicked += () =>
            {
                m_PreviewMode.Value = m_PreviewMode.Value == PreviewMode.Disabled
                    ? PreviewMode.Enabled
                    : PreviewMode.Disabled;
            };
            if (Application.isPlaying)
            {
                m_TogglePreviewButton.style.display = DisplayStyle.None;
            }

            return m_TogglePreviewButton;
        }

        protected override async UniTask OnEnableAsync(Signal cancellationSignal)
        {
            m_Area2D = (GameArea2D)target;
            await WhenInspectorGUIIsCreated();
            m_PreviewMode.Value = m_Area2D.isActiveInEditor ? PreviewMode.Enabled : PreviewMode.Disabled;
            using var previewModeListener = m_PreviewMode.WatchAsync(OnPreviewModeChange);
            await cancellationSignal;
        }

        private UniTaskVoid OnPreviewModeChange(PreviewMode mode, Signal cancellationSignal)
        {
            if (mode == PreviewMode.Enabled)
            {
                m_TogglePreviewButton.AddToClassList("MetamaticTailwind_BackgroundColorIndicatorActive");
                m_TogglePreviewButton.AddToClassList("MetamaticTailwind_TextColorInverse");
                m_Area2D.isActiveInEditor = true;
                m_Area2D.EditorOnlyApplyActivation();
                EditorUtility.SetDirty(m_Area2D);
            }
            else
            {
                m_TogglePreviewButton.RemoveFromClassList("MetamaticTailwind_BackgroundColorIndicatorActive");
                m_TogglePreviewButton.RemoveFromClassList("MetamaticTailwind_TextColorInverse");
                m_Area2D.isActiveInEditor = false;
                m_Area2D.EditorOnlyApplyActivation();
                EditorUtility.SetDirty(m_Area2D);
            }

            return new UniTaskVoid();
        }

        private void OnSceneGUI()
        {
            Transform t = m_Area2D.transform;
            // bounds.center — локальные координаты
            Vector2 localCenter = m_Area2D.bounds.center;
            // Вычисляем мировой центр
            Vector2 worldCenter = (Vector2)t.position + localCenter;
            Vector2 size = m_Area2D.bounds.size;

            Color boundsColor = new Color(0.39f, 1f, 0.63f);
            Handles.color = boundsColor;

            // Рисуем прямоугольник, используя мировой центр
            Handles.DrawWireCube(worldCenter, size);

            // Рисуем название объекта по центру bounds
            GUIStyle labelStyle = new GUIStyle();
            labelStyle.fontSize = 20;
            labelStyle.normal.textColor = boundsColor;
            labelStyle.alignment = TextAnchor.MiddleCenter;
            Handles.Label(worldCenter, m_Area2D.name, labelStyle);

            // Вычисляем позиции захватов в мировых координатах
            Vector2[] handlePositions = new Vector2[]
            {
                worldCenter + new Vector2(-size.x / 2, 0), // Левый захват
                worldCenter + new Vector2(size.x / 2, 0), // Правый захват
                worldCenter + new Vector2(0, size.y / 2), // Верхний захват
                worldCenter + new Vector2(0, -size.y / 2) // Нижний захват
            };

            float handleSize = 0.2f;

            for (int i = 0; i < handlePositions.Length; i++)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 newHandlePos = Handles.FreeMoveHandle(handlePositions[i], handleSize,
                    Vector3.zero, Handles.SphereHandleCap);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(m_Area2D, "Resize Area");

                    // При редактировании вычисляем новый мировой центр и преобразуем его в локальные координаты
                    if (i == 0) // Левый захват: фиксируем правую грань
                    {
                        float leftBoundary = newHandlePos.x;
                        float rightBoundary = worldCenter.x + size.x / 2;
                        float newSizeX = rightBoundary - leftBoundary;
                        float newCenterXWorld = leftBoundary + newSizeX / 2;
                        m_Area2D.bounds.size = new Vector2(newSizeX, size.y);
                        m_Area2D.bounds.center = new Vector2(newCenterXWorld - t.position.x, m_Area2D.bounds.center.y);
                    }
                    else if (i == 1) // Правый захват: фиксируем левую грань
                    {
                        float leftBoundary = worldCenter.x - size.x / 2;
                        float rightBoundary = newHandlePos.x;
                        float newSizeX = rightBoundary - leftBoundary;
                        float newCenterXWorld = leftBoundary + newSizeX / 2;
                        m_Area2D.bounds.size = new Vector2(newSizeX, size.y);
                        m_Area2D.bounds.center = new Vector2(newCenterXWorld - t.position.x, m_Area2D.bounds.center.y);
                    }
                    else if (i == 2) // Верхний захват: фиксируем нижнюю грань
                    {
                        float bottomBoundary = worldCenter.y - size.y / 2;
                        float topBoundary = newHandlePos.y;
                        float newSizeY = topBoundary - bottomBoundary;
                        float newCenterYWorld = bottomBoundary + newSizeY / 2;
                        m_Area2D.bounds.size = new Vector2(size.x, newSizeY);
                        m_Area2D.bounds.center = new Vector2(m_Area2D.bounds.center.x, newCenterYWorld - t.position.y);
                    }
                    else if (i == 3) // Нижний захват: фиксируем верхнюю грань
                    {
                        float topBoundary = worldCenter.y + size.y / 2;
                        float bottomBoundary = newHandlePos.y;
                        float newSizeY = topBoundary - bottomBoundary;
                        float newCenterYWorld = bottomBoundary + newSizeY / 2;
                        m_Area2D.bounds.size = new Vector2(size.x, newSizeY);
                        m_Area2D.bounds.center = new Vector2(m_Area2D.bounds.center.x, newCenterYWorld - t.position.y);
                    }

                    EditorUtility.SetDirty(m_Area2D);
                }
            }
        }
    }
}