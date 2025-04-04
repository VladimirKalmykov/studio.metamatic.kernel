using System;
using Cysharp.Threading.Tasks;
using Metamatic.EditorTools;
using Metamatic.UIToolKit;
using Metamatic.UIToolKit.VisualElements;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Metamatic.Framework.Workshop
{
    [CustomEditor(typeof(MultiplePointsSpawnArea))]
    public class MultiplePointsSpawnAreaEditor : AsyncOdinEditor
    {
        [Flags]
        public enum Mode
        {
            None = 0,
            Edit = 1 << 0,
            Add = 1 << 1
        }

        private ToggleButton m_ToggleEditButton;
        private ToggleButton m_AddPointButton;
        private Button m_DeletePointButton;

        private MultiplePointsSpawnArea spawnArea => target as MultiplePointsSpawnArea;

        private ObservableEnum<Mode> m_ObservableMode = new ObservableEnum<Mode>();
        private string m_SelectedPointId;
        private const float k_HandleSize = 0.15f;

        protected override VisualElement CreateInspectorGUIAsync()
        {
            var root = new VisualElement();
            root.AddToClassList("Root");
            root.styleSheets.Add(UIToolKitCore.tailwindStyleSheet);
            root.styleSheets.Add(UIToolKitCore.styleSheet);

            var ugui = new IMGUIContainer(OnInspectorGUI);
            root.Add(ugui);
            root.Add(CreateControlsElement());

            return root;
        }

        private VisualElement CreateControlsElement()
        {
            var controls = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginBottom = 5
                }
            };

            m_ToggleEditButton = new ToggleButton { text = "Edit Points" };
            m_ToggleEditButton.clicked += () =>
            {
                m_ObservableMode.value = m_ObservableMode.HasFlag(Mode.Edit) ? Mode.None : Mode.Edit;
                SceneView.RepaintAll();
            };

            m_AddPointButton = new ToggleButton { text = "Add Point" };
            m_AddPointButton.clicked += () =>
            {
                m_ObservableMode.value = m_ObservableMode.HasFlag(Mode.Add) ? Mode.None : Mode.Add;
                SceneView.RepaintAll();
            };

            m_DeletePointButton = new Button { text = "Delete Selected", style = { display = DisplayStyle.None } };
            m_DeletePointButton.clicked += DeleteSelectedPoint;

            controls.Add(m_ToggleEditButton);
            controls.Add(m_AddPointButton);
            controls.Add(m_DeletePointButton);

            if (Application.isPlaying)
            {
                controls.style.display = DisplayStyle.None;
            }

            return controls;
        }

        protected override async UniTask OnEnableAsync(Signal cancellationSignal)
        {
            await WhenInspectorGUIIsCreated();
            using var observeMode = m_ObservableMode.Watch(OnModeChange);
            SceneView.duringSceneGui += OnSceneViewGUI;
            await cancellationSignal;
            SceneView.duringSceneGui -= OnSceneViewGUI;
        }

        private void OnModeChange(Mode mode)
        {
            m_ToggleEditButton.value = mode.HasFlag(Mode.Edit);
            m_AddPointButton.value = mode.HasFlag(Mode.Add);
            SceneView.RepaintAll();
        }

        private void OnSceneViewGUI(SceneView sceneView)
        {
            if (spawnArea == null) return;

            Event e = Event.current;
            if (m_ObservableMode.HasFlag(Mode.Add) && e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                Plane plane = new Plane(-sceneView.camera.transform.forward, spawnArea.transform.position);
                if (plane.Raycast(ray, out float distance))
                {
                    var localPosition = spawnArea.transform.InverseTransformPoint(ray.GetPoint(distance));
                    Undo.RecordObject(spawnArea, "Add Spawnpoint");
                    spawnArea.CreateSpawnpoint(localPosition, Quaternion.identity);
                    EditorUtility.SetDirty(spawnArea);
                }
                e.Use();
            }

            foreach (var spawnpoint in spawnArea.GetSpawnpoints())
            {
                DrawSpawnPoint(spawnpoint);
            }
        }

        private void DrawSpawnPoint(Spawnpoint spawnpoint)
        {
            Vector3 worldPosition = spawnArea.transform.TransformPoint(spawnpoint.position);
            float size = HandleUtility.GetHandleSize(worldPosition) * k_HandleSize;

            // Рамка квадрата без fill
            Handles.color = spawnpoint.id == m_SelectedPointId ? Color.yellow : spawnArea.devToolsColor;
            Handles.DrawWireCube(worldPosition, Vector3.one * size);

            // Палочка (facing)
            Vector3 direction = spawnpoint.rotation * Vector3.right;
            Handles.DrawLine(worldPosition, worldPosition + direction * size * 4);

            // Закрашивание выделенной точки
            if (spawnpoint.id == m_SelectedPointId)
            {
                Handles.color = new Color(Handles.color.r, Handles.color.g, Handles.color.b, 0.2f);
                Handles.CubeHandleCap(0, worldPosition, Quaternion.identity, size, EventType.Repaint);
            }

            // Кнопка выбора точки
            if (m_ObservableMode.HasFlag(Mode.Edit))
            {
                if (Handles.Button(worldPosition, Quaternion.identity, size, size, Handles.RectangleHandleCap))
                {
                    m_SelectedPointId = spawnpoint.id;
                    m_DeletePointButton.style.display = DisplayStyle.Flex;
                    Repaint();
                }

                // Хендл для перемещения выделенной точки
                if (spawnpoint.id == m_SelectedPointId)
                {
                    EditorGUI.BeginChangeCheck();
                    var newPosition = Handles.PositionHandle(worldPosition, Quaternion.identity);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(spawnArea, "Move Spawnpoint");
                        spawnpoint.position = spawnArea.transform.InverseTransformPoint(newPosition);
                        EditorUtility.SetDirty(spawnArea);
                    }
                }
            }
        }

        private void DeleteSelectedPoint()
        {
            if (string.IsNullOrEmpty(m_SelectedPointId)) return;

            Undo.RecordObject(spawnArea, "Delete Spawnpoint");
            spawnArea.RemoveSpawnpoint(m_SelectedPointId);
            m_SelectedPointId = null;
            m_DeletePointButton.style.display = DisplayStyle.None;
            EditorUtility.SetDirty(spawnArea);
            SceneView.RepaintAll();
        }
    }
}
