using System;
using Cysharp.Threading.Tasks;
using Metamatic.EditorTools;
using Metamatic.UIToolKit;
using Metamatic.UIToolKit.VisualElements;
using UnityEditor;
using UnityEditor.UIElements;
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

            var container = new VisualElement();
            container.AddToClassList(UIToolKitCore.Tailwind.Column);
            container.style.marginTop = 12;

            container.Add(CreateControlsElement());
            container.Add(CreateGridControlElement());

            root.Add(container);

            return root;
        }

        private VisualElement CreateControlsElement()
        {
            var controls = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    marginBottom = 6
                }
            };
            
            var gridControls = new VisualElement()
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginBottom = 6
                }
            };

            // Toggle для отображения грида
            var gridToggle = new Toggle("Show grid");
            gridToggle.value = spawnArea.editor_ShowGrid;
            gridToggle.RegisterValueChangedCallback(evt =>
            {
                var settings = spawnArea.editor_GridSettings;
                if (settings.gridSize.x == 0 && settings.gridSize.y == 0)
                {
                    settings.gridSize = new Vector2(3, 3);
                    settings.appearance = Grid2DSettings.AppearanceType.Dots;
                }

                if (settings.color == default)
                {
                    settings.color = new Color(0f, 0.37f, 1f, 0.64f);
                }

                if (settings.cellSize.x == 0 && settings.cellSize.y == 0)
                {
                    settings.cellSize = new Vector2(1f, 1f);
                }

                spawnArea.editor_GridSettings = settings;
                spawnArea.editor_ShowGrid = evt.newValue;
                SceneView.RepaintAll();
            });
            
            gridControls.Add(gridToggle);

            // Toggle для Snap to grid
            var snapToggle = new Toggle("Snap to grid")
            {
                value = spawnArea.editor_SnapToGrid,
                style =
                {
                    marginLeft = 6
                }
            };
            snapToggle.RegisterValueChangedCallback(evt =>
            {
                spawnArea.editor_SnapToGrid = evt.newValue;
                EditorUtility.SetDirty(spawnArea);
                SceneView.RepaintAll();
            });
            gridControls.Add(snapToggle);

            var modesControls = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginBottom = 6
                }
            };
            
            // Toggle Edit Mode
            m_ToggleEditButton = new ToggleButton { text = "Edit Points" };
            m_ToggleEditButton.clicked += () =>
            {
                m_ObservableMode.value = m_ObservableMode.HasFlag(Mode.Edit) ? Mode.None : Mode.Edit;
                SceneView.RepaintAll();
            };
            modesControls.Add(m_ToggleEditButton);

            // Toggle Add Mode
            m_AddPointButton = new ToggleButton { text = "Add Point" };
            m_AddPointButton.clicked += () =>
            {
                m_ObservableMode.value = m_ObservableMode.HasFlag(Mode.Add) ? Mode.None : Mode.Add;
                SceneView.RepaintAll();
            };
            modesControls.Add(m_AddPointButton);

            // Delete Point Button
            m_DeletePointButton = new Button { text = "Delete Selected", style = { display = DisplayStyle.None } };
            m_DeletePointButton.clicked += DeleteSelectedPoint;
            m_DeletePointButton.tooltip = "Delete selected point";
            modesControls.Add(m_DeletePointButton);
            

            controls.Add(modesControls);
            controls.Add(gridControls);

            if (Application.isPlaying)
            {
                controls.style.display = DisplayStyle.None;
            }

            return controls;
        }

        private VisualElement CreateGridControlElement()
        {
            var gridControls = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    marginBottom = 6
                }
            };

            var gridSettings = new GridSettingsBoxGroup();
            gridSettings.OnValueChanged += settings =>
            {
                spawnArea.editor_GridSettings = settings;
                EditorUtility.SetDirty(spawnArea);
                SceneView.RepaintAll();
            };
            gridSettings.style.display = spawnArea.editor_ShowGrid ? DisplayStyle.Flex : DisplayStyle.None;
            gridSettings.style.marginLeft = 3;
            gridSettings.value = spawnArea.editor_GridSettings;
            gridControls.Add(gridSettings);

            return gridControls;
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

            if (spawnArea.editor_ShowGrid)
            {
                EditorSceneGuiHelpers.DrawGrid2D(spawnArea.transform.position, spawnArea.editor_GridSettings);
            }

            Event e = Event.current;

            // Принудительная перерисовка при движении мыши
            if (e.type == EventType.MouseMove)
                SceneView.RepaintAll();

            bool isInInteractionMode = m_ObservableMode.HasFlag(Mode.Add) ||
                                       (m_ObservableMode.HasFlag(Mode.Edit) &&
                                        !string.IsNullOrEmpty(m_SelectedPointId));

            if (isInInteractionMode)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                Plane gridPlane;

                if (spawnArea.editor_ShowGrid)
                {
                    var settings = spawnArea.editor_GridSettings;
                    Vector3 gridCenter = spawnArea.transform.position + settings.offset;
                    Vector3 gridNormal = Quaternion.Euler(settings.rotation) * Vector3.forward;
                    gridPlane = new Plane(gridNormal, gridCenter);
                }
                else
                {
                    gridPlane = new Plane(-sceneView.camera.transform.forward, spawnArea.transform.position);
                }

                if (gridPlane.Raycast(ray, out float distance))
                {
                    Vector3 worldMousePosition = ray.GetPoint(distance);
                    Vector3 snappedPosition = spawnArea.editor_SnapToGrid
                        ? SnapPositionToGrid(worldMousePosition)
                        : spawnArea.transform.InverseTransformPoint(worldMousePosition);

                    Vector3 predictorWorldPosition = spawnArea.transform.TransformPoint(snappedPosition);
                    float predictorSize = HandleUtility.GetHandleSize(predictorWorldPosition) * k_HandleSize;

                    Handles.color = new Color(0f, 1f, 1f, 0.5f);
                    Handles.CubeHandleCap(0, predictorWorldPosition, Quaternion.identity, predictorSize,
                        EventType.Repaint);

                    // Логика создания новой точки
                    if (m_ObservableMode.HasFlag(Mode.Add) && e.type == EventType.MouseDown && e.button == 0 && !e.alt)
                    {
                        Undo.RecordObject(spawnArea, "Add Spawnpoint");
                        spawnArea.CreateSpawnpoint(snappedPosition, Quaternion.identity);
                        EditorUtility.SetDirty(spawnArea);
                        e.Use();
                    }
                }
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

            Handles.color = spawnpoint.id == m_SelectedPointId ? Color.yellow : spawnArea.devToolsColor;
            Handles.DrawWireCube(worldPosition, Vector3.one * size);

            Vector3 direction = spawnpoint.rotation * Vector3.right;
            Handles.DrawLine(worldPosition, worldPosition + direction * size * 4);

            if (spawnpoint.id == m_SelectedPointId)
            {
                Handles.color = new Color(Handles.color.r, Handles.color.g, Handles.color.b, 0.2f);
                Handles.CubeHandleCap(0, worldPosition, Quaternion.identity, size, EventType.Repaint);
            }

            if (m_ObservableMode.HasFlag(Mode.Edit))
            {
                if (Handles.Button(worldPosition, Quaternion.identity, size, size, Handles.RectangleHandleCap))
                {
                    m_SelectedPointId = spawnpoint.id;
                    m_DeletePointButton.style.display = DisplayStyle.Flex;
                    Repaint();
                }

                if (spawnpoint.id == m_SelectedPointId)
                {
                    EditorGUI.BeginChangeCheck();
                    Vector3 newWorldPos = Handles.PositionHandle(worldPosition, Quaternion.identity);

                    // Predictor только при движении точки
                    Vector3 snappedPos = spawnArea.editor_SnapToGrid
                        ? SnapPositionToGrid(newWorldPos)
                        : spawnArea.transform.InverseTransformPoint(newWorldPos);

                    Vector3 predictorWorldPosition = spawnArea.transform.TransformPoint(snappedPos);
                    float predictorSize = HandleUtility.GetHandleSize(predictorWorldPosition) * k_HandleSize;

                    Handles.color = new Color(0f, 1f, 1f, 0.5f);
                    Handles.CubeHandleCap(0, predictorWorldPosition, Quaternion.identity, predictorSize,
                        EventType.Repaint);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(spawnArea, "Move Spawnpoint");
                        spawnpoint.position = snappedPos;
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

        /// <summary>
        /// Вычисляет позицию, «прилипшую» к ближайшему узлу сетки, если расстояние до узла меньше threshold.
        /// Логика дублирует построение грида:
        /// 1. Берется центр грида = spawnArea.transform.position + editor_GridSettings.offset.
        /// 2. Создается матрица преобразования с указанным поворотом.
        /// 3. Позиция переводится в локальные координаты, округляется до кратного cellSize,
        ///    и если расстояние до узла меньше snapThreshold, позиция корректируется.
        /// 4. Результат возвращается в локальные координаты spawnArea.
        /// </summary>
        /// <param name="worldPosition">Исходная позиция в мировых координатах.</param>
        /// <returns>Снапнутая позиция в локальных координатах spawnArea.</returns>
        private Vector3 SnapPositionToGrid(Vector3 worldPosition)
        {
            var settings = spawnArea.editor_GridSettings;
            Vector3 gridCenter = spawnArea.transform.position + settings.offset;
            Quaternion gridRotation = Quaternion.Euler(settings.rotation);
            Matrix4x4 gridMatrix = Matrix4x4.TRS(gridCenter, gridRotation, Vector3.one);

            Vector3 localGridPos = gridMatrix.inverse.MultiplyPoint3x4(worldPosition);

            localGridPos.x = Mathf.Round(localGridPos.x / settings.cellSize.x) * settings.cellSize.x;
            localGridPos.y = Mathf.Round(localGridPos.y / settings.cellSize.y) * settings.cellSize.y;

            Vector3 snappedWorld = gridMatrix.MultiplyPoint3x4(localGridPos);
            return spawnArea.transform.InverseTransformPoint(snappedWorld);
        }
    }
}