#if UNITY_EDITOR
using Cysharp.Threading.Tasks;
using Metamatic;
using Metamatic.EditorTools;
using Metamatic.UIToolKit;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic.Framework.Workshop
{
    [CustomEditor(typeof(GameArea3D))]
    public class GameArea3DEditor : AsyncOdinEditor
    {
        private GameArea3D      m_Area;
        private Button          m_ToggleEditBtn;
        private readonly BoxBoundsHandle m_BoundsHandle = new();

        private GameArea3D area => (GameArea3D)target;

        // ---------- Inspector UI (UI Toolkit) ------------------------------

        protected override VisualElement CreateInspectorGUIAsync()
        {
            var root = new VisualElement();
            root.AddToClassList("Root");
            root.styleSheets.Add(UIToolKitCore.tailwindStyleSheet);
            root.styleSheets.Add(UIToolKitCore.styleSheet);

            root.Add(CreateIdElement());

            root.Add(new IMGUIContainer(() =>
            {
                if (target != null) OnInspectorGUI();        // обычные одно-строчные поля Unity
            }));

            root.Add(CreateControlsElement());
            root.Add(CreateEntitiesListElement());
            return root;
        }

        private VisualElement CreateIdElement()
        {
            var idField = new IdEditorElement(() =>
            {
                return IdsGenerator.GenerateUniqueIdForComponent(
                           area, IdsGenerator.IdType.ShortGuid,
                           (a, id) => a.id == id, out var id) ? id : "";
            })
            { value = area.id };

            idField.onValueChanged += v =>
            {
                area.id = v;
                EditorUtility.SetDirty(area);
            };
            return idField;
        }

        private VisualElement CreateControlsElement()
        {
            m_ToggleEditBtn = new Button { text = "Activate editor" };
            m_ToggleEditBtn.clicked += () => area.EditorOnlyEditMode.Value = !area.EditorOnlyEditMode.Value;
            if (Application.isPlaying) m_ToggleEditBtn.style.display = DisplayStyle.None;
            return m_ToggleEditBtn;
        }

        private VisualElement CreateEntitiesListElement()
        {
            var container = new VisualElement();
            container.AddToClassList(UIToolKitCore.Tailwind.PaddingTop12);
            container.AddToClassList(UIToolKitCore.Tailwind.Column);

            var title = new Label("Entities");
            title.AddToClassList(UIToolKitCore.Tailwind.TextSizeMd);
            title.AddToClassList(UIToolKitCore.Tailwind.MarginBottom6);
            container.Add(title);

            var list = new TableView<Entity>(new TableView<Entity>.Col[]
            {
                new("Name",     TableView<Entity>.Col.WidthType.Fixed,    80f,  e => new Label(e.name)),
                new("Id",       TableView<Entity>.Col.WidthType.Fixed,   120f,  e => new Label(e.id)),
                new("IsStatic", TableView<Entity>.Col.WidthType.Relative, 1f,   e => new Label(e.isStatic ? "true":"false")),
            });

            list.AddToClassList("EntitiesList");
            list.Bind(area.EditorObservableEntities);
            container.Add(list);
            return container;
        }

        // ---------- AsyncOdin lifecycle ------------------------------------

        protected override async UniTask OnEnableAsync(Signal cancellation)
        {
            m_Area = (GameArea3D)target;
            await WhenInspectorGUIIsCreated();
            area.EditorOnlyEditMode.Value = m_Area.isActiveInEditor;

            using var _ = area.EditorOnlyEditMode.WatchAsync(OnPreviewToggle);
            await cancellation;
        }

        private UniTaskVoid OnPreviewToggle(bool enabled, Signal _)      // UI-decorate
        {
            const string activeBg = "MetamaticTailwind_BackgroundColorIndicatorActive";
            const string activeFg = "MetamaticTailwind_TextColorInverse";

            if (enabled)
            {
                m_ToggleEditBtn.AddToClassList(activeBg);
                m_ToggleEditBtn.AddToClassList(activeFg);
                m_Area.isActiveInEditor = true;
            }
            else
            {
                m_ToggleEditBtn.RemoveFromClassList(activeBg);
                m_ToggleEditBtn.RemoveFromClassList(activeFg);
                m_Area.isActiveInEditor = false;
            }

            m_Area.EditorOnlyApplyActivation();
            EditorUtility.SetDirty(m_Area);
            return default;
        }

        // ---------- Scene view editing -------------------------------------

        private void OnSceneGUI()
        {
            if (!area.EditorOnlyEditMode.Value) return;

            var t = area.transform;

            // Рисуем в локальной системе объекта
            using (new Handles.DrawingScope(t.localToWorldMatrix))
            {
                m_BoundsHandle.center = area.bounds.center;
                m_BoundsHandle.size   = area.bounds.size;
                Handles.color         = new Color(0.39f, 1f, 0.63f);

                EditorGUI.BeginChangeCheck();
                m_BoundsHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(area, "Resize GameArea3D");
                    area.bounds.center = m_BoundsHandle.center;
                    area.bounds.size   = m_BoundsHandle.size;
                    EditorUtility.SetDirty(area);
                }

                // подпись
                var style = new GUIStyle
                {
                    fontSize  = 20,
                    alignment = TextAnchor.MiddleCenter,
                    normal    = { textColor = Handles.color }
                };
                Handles.Label(area.bounds.center, area.name, style);
            }
        }
    }
}
#endif
