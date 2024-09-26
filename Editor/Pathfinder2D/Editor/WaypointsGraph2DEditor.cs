using System;
using Cysharp.Threading.Tasks;
using Metamatic;
using Metamatic.EditorTools;
using Metamatic.Pathfinder;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic
{
    [CustomEditor(typeof(WaypointsGraph2D))]
    public class WaypointsGraph2DEditor: AsyncEditor
    {
        private readonly Observable<bool> m_IsEditorMode = new Observable<bool>();
        
        #region Visual elements
        private EnumField m_SelectWaypointTypeField;
        private Label m_WaypointsCountLabel;
        private Button m_GeneratingButton;
        #endregion
        
        private WaypointType m_SelectedWaypointType;
        private WaypointsGraph2D m_Graph2D;
        private bool m_LazyGeneration = true;
        protected override async UniTask OnEnableAsync(CancellationSignal cancellationSignal)
        {
            Console.Debug("WaypointsGraph2DEditor", "Enabled");
            m_Graph2D = (WaypointsGraph2D)target;
            using var watchEditorEnabled = m_IsEditorMode.Watch(OnEditorModeSwitched);
            using var isGeneratingWatcher = m_Graph2D.isGenerating.Watch(OnGeneratingChange);
            SceneView.duringSceneGui += OnSceneGUI; // Подписка на события SceneView
            m_Graph2D.OnWaypointsChanged += OnWaypointsChange;
            await cancellationSignal;
            m_Graph2D.OnWaypointsChanged -= OnWaypointsChange;
            SceneView.duringSceneGui -= OnSceneGUI; // Подписка на события SceneView
            Console.Debug("WaypointsGraph2DEditor", "Disalbed");
        }
        
        private void OnGeneratingChange(bool isGenerating)
        {
            if (m_GeneratingButton == null) return;
            if (isGenerating)
            {
                m_GeneratingButton.AddToClassList("Active");
                m_GeneratingButton.text = "Cancel Generation";
            }
            else
            {
                m_GeneratingButton.RemoveFromClassList("Active");
                m_GeneratingButton.text = "Generate Grid";
            }
        }
        
        private void OnWaypointsChange()
        {
            if (m_Graph2D != null)
            {
                m_WaypointsCountLabel.text = $"Waypoints Count: {m_Graph2D.waypointsCount}";
            }
        }
        
        private void OnEditorModeSwitched(bool enabled)
        {
            if (m_SelectWaypointTypeField != null)
            {
                m_SelectWaypointTypeField.enabledSelf = enabled;
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            Console.Debug("WaypointsGraph2DEditor", "CreateInspectorGUI");
            var superRoot = new VisualElement();
            superRoot.AddToClassList("Root");
            
            VisualElement ugui = new UnityEngine.UIElements.IMGUIContainer(() => {
                if (target != null) {
                    OnInspectorGUI();
                }
            });
            
            superRoot.Add(ugui);
            superRoot.Add(BuildEditorGUI());
            return superRoot;
        }
        
        private VisualElement BuildEditorGUI()
        {
            var root = new VisualElement();
            root.AddToClassList("Panel");
            
            /* Connect uss to root stylesheet */
            root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/studio.metamatic.kernel/Editor/Pathfinder2D/Editor/WaypointsGraph2D.uss"));
            
            /* Create field that shows current waypoints count */
            var row1 = CreateRow();
            m_WaypointsCountLabel = new Label($"Waypoints Count: {m_Graph2D.waypointsCount}");
            row1.Add(m_WaypointsCountLabel);
            root.Add(row1);
            
            /* Create row with select waypoint type dropdown */
            var selectWaypointTypeRow = CreateRow();
            Label selectWaypointTypeLabel = new Label("Select Waypoint Type:");
            selectWaypointTypeRow.Add(selectWaypointTypeLabel);
            /* Create dropdown for enum WaypointType */
            m_SelectWaypointTypeField = new EnumField(WaypointType.Default);
            m_SelectWaypointTypeField.enabledSelf = false;
            /* Listen to changes */
            m_SelectWaypointTypeField.RegisterValueChangedCallback(evt =>
            {
                m_SelectedWaypointType = (WaypointType) evt.newValue;
            });
            selectWaypointTypeRow.Add(m_SelectWaypointTypeField);
            root.Add(selectWaypointTypeRow);
            
           
            var editorFunctionsRow = CreateRow();
            editorFunctionsRow.AddToClassList("HorizontalGroup");
            /* Create toggle editor mode button */
            Button enableEditorButton = default;
            enableEditorButton = new Button(() =>
            {
                m_IsEditorMode.value = !m_IsEditorMode.value;
                if (m_IsEditorMode.value)
                {
                    enableEditorButton.text = "Disable Editor";
                    enableEditorButton.AddToClassList("Active");
                }
                else
                {
                    enableEditorButton.text = "Enable Editor";
                    enableEditorButton.RemoveFromClassList("Active");
                }
            });
            enableEditorButton.text = "Enable Editor";
            enableEditorButton.AddToClassList("ToggleButton");
            
            /* Create clear waypoints button */
            var clearButton = new Button(() =>
            {
                m_Graph2D.ClearWaypoints();
            });
            clearButton.text = "Clear Waypoints";
            clearButton.AddToClassList("Button");
            editorFunctionsRow.Add(clearButton);
            editorFunctionsRow.Add(enableEditorButton);
            
            /* Create recalculate edges button */
            var recalculateEdgesButton = new Button(() =>
            {
                m_Graph2D.RecalculateAllEdges();
            });
            recalculateEdgesButton.text = "Recalculate Edges";
            recalculateEdgesButton.AddToClassList("Button");
            editorFunctionsRow.Add(recalculateEdgesButton);
            
            /* Generator row */
            var generatorRow = CreateRow();
            generatorRow.AddToClassList("HorizontalGroup");
            
            /* Add toggle */
            var toggle = new Toggle("Lazy");
            toggle.value = m_LazyGeneration;
            toggle.RegisterValueChangedCallback(evt =>
            {
                m_LazyGeneration = evt.newValue;
            });
            generatorRow.Add(toggle);
            
            /* Generate button */
            m_GeneratingButton = new Button(() =>
            {
                if (m_Graph2D.isGenerating.value)
                {
                    m_Graph2D.CancelGeneration();
                }
                else
                {
                    m_Graph2D.GenerateWaypoints(false, lazy: m_LazyGeneration).Forget();
                }
            });

            m_GeneratingButton.AddToClassList("ToggleButton");
            if (m_Graph2D.isGenerating.value)
            {
                m_GeneratingButton.AddToClassList("Active");
                m_GeneratingButton.text = "Cancel Generation";
            }
            else
            {
                m_GeneratingButton.RemoveFromClassList("Active");
                m_GeneratingButton.text = "Generate Grid";
            }
            generatorRow.Add(m_GeneratingButton);
            
            root.Add(editorFunctionsRow);
            root.Add(generatorRow);
            
            return root;
        }

        private VisualElement CreateRow()
        {
            var row = new VisualElement();
            row.AddToClassList("Row");
            return row;
        }
        
        private void OnSceneGUI(SceneView sceneView)
        {
            if (m_IsEditorMode.value)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive)); // Блокировка выбора других объектов

                // Устанавливаем стандартный курсор крест
                EditorGUIUtility.AddCursorRect(new Rect(0, 0, sceneView.position.width, sceneView.position.height), MouseCursor.ArrowPlus);

                Event e = Event.current;

                if (e.type == EventType.MouseDown && e.button == 0) // ЛКМ клик
                {
                    Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        // Создаем вейпоинт в месте пересечения с поверхностью
                        CreateWaypointAtPosition(hit.point);
                    }
                    else
                    {
                        // Если поверхность не найдена, создаем вейпоинт в свободном пространстве
                        Vector3 worldPosition = ray.origin + ray.direction * 10f; // Задаем произвольную глубину
                        CreateWaypointAtPosition(worldPosition);
                    }

                    e.Use(); // Указываем, что событие было обработано
                }
            }
        }

        private void CreateWaypointAtPosition(Vector3 position)
        {
            var waypointGraph = (WaypointsGraph2D)target;
            waypointGraph.AddWaypoint(new Waypoint2D(m_Graph2D.transform.InverseTransformPoint(position), m_SelectedWaypointType));

            // Обновляем сцену для отображения изменений
            EditorUtility.SetDirty(waypointGraph);
        }
    }
}