using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic.Framework
{
    [UxmlElement]
    public partial class Panel: VisualElement
    {
        [Version("0.1")]
        [Serializable]
        public struct UserPreferences
        {
            public Vector2 Position;
            public Vector2 Size;
            
            public UserPreferences(Vector2 position, Vector2 size)
            {
                Position = position;
                Size = size;
            }
        }
        public struct Constraints
        {
            public UserPreferences DefaultPreferences;
            public Vector2 MinSize;
            public Vector2 MaxSize;
            
            public Constraints(UserPreferences defaultPreferences, Vector2 minSize, Vector2 maxSize)
            {
                DefaultPreferences = defaultPreferences;
                MinSize = minSize;
                MaxSize = maxSize;
            }
        }
        public readonly string PanelName;
        private VisualElement m_Header;
        private Label m_Title;
        private Button m_RightButton;
        private ScrollView m_Content;
        private event Action OnDetachFromPanelEvent;
        private ObservableReadOnly<MetamaticFrameworkOverlay> m_OverlayContext;
        private UserPreferences m_Preferences;
        private Constraints m_Constraints;
        
        public override VisualElement contentContainer => m_Content;
        
        public string title
        {
            get => m_Title.text;
            set => m_Title.text = value;
        }
        
        public Panel(): this("NewPanel", "New panel")
        {
        }
        public Panel(string name, string title, Constraints constraints = default)
        {
            this.PanelName = name;
            m_Constraints = constraints;
            AddToClassList("Panel");
            m_Header = new VisualElement();
            m_Header.AddToClassList("PanelHeader");
            
            m_Title = new Label("Panel");
            m_Title.AddToClassList("PanelTitle");
            m_Title.text = title;
            m_Header.Add(m_Title);
            
            var spacer = new VisualElement();
            spacer.AddToClassList("PanelHeaderSpacer");
            m_Header.Add(spacer);
            
            m_RightButton = new Button(() => { });
            m_RightButton.AddToClassList("PanelHeaderRightButton");
            m_Header.Add(m_RightButton);

            m_Content = new ScrollView
            {
                name = "unity-content-container"
            };
            m_Content.AddToClassList("PanelContent");
            
            hierarchy.Add(m_Header);
            hierarchy.Add(m_Content);
            
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }
        
        private void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            m_OverlayContext = this.GetContext<MetamaticFrameworkOverlay>();
            
            /* Load preferences */
            m_Preferences = m_OverlayContext.Value.GetPanelPreferences(PanelName, m_Constraints.DefaultPreferences);
            m_OverlayContext.Value.uiDocument.rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            
            ApplyPreferences(m_Preferences);
            
            InitDraggable();
        }
        
        private void OnRootGeometryChanged(GeometryChangedEvent evt)
        {
            ApplyPreferences(m_Preferences);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            m_OverlayContext.Value.uiDocument.rootVisualElement.UnregisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            OnDetachFromPanelEvent?.Invoke();
        }
        
        private void ApplyPreferences(UserPreferences preferences)
        {
            var finalPosition = preferences.Position;
            var finalSize = preferences.Size;
            /* Apply constraints */
            if (m_Constraints.MinSize != Vector2.zero)
            {
                finalSize = Vector2.Max(finalSize, m_Constraints.MinSize);
            }
            if (m_Constraints.MaxSize.x != 0)
            {
                finalSize.x = Mathf.Min(finalSize.x, m_Constraints.MaxSize.x);
            }
            if (m_Constraints.MaxSize.y != 0)
            {
                finalSize.y = Mathf.Min(finalSize.y, m_Constraints.MaxSize.y);
            }
            /* Apply constraints relative screen size. Panel should not be outside of the screen () */
            var overlay = m_OverlayContext.Value;
            finalPosition = Vector2.Max(finalPosition, Vector2.zero);
            var uiDocumentWidth = overlay.uiDocument.rootVisualElement.layout.width;
            var uiDocumentHeight = overlay.uiDocument.rootVisualElement.layout.height;
            if (!float.IsNaN(uiDocumentWidth) && !float.IsNaN(uiDocumentHeight))
            {
                finalPosition = new Vector2(
                    Mathf.Min(finalPosition.x, uiDocumentWidth - finalSize.x),
                    Mathf.Min(finalPosition.y, uiDocumentHeight - finalSize.y)
                );
            }
            
            /* Apply preferences */
            style.left = finalPosition.x;
            style.top = finalPosition.y;
            style.width = finalSize.x;
            style.height = finalSize.y;
        }
        
        private void WritePreferences()
        {
            var preferences = new UserPreferences
            {
                Position = new Vector2(resolvedStyle.left, resolvedStyle.top),
                Size = new Vector2(resolvedStyle.width, resolvedStyle.height)
            };
            m_OverlayContext.Value.SetPanelPreferences(PanelName, preferences);
        }

        private void InitDraggable()
        {
            var startMousePosition = Vector2.zero;
            var startPanelPosition = Vector2.zero;

            void OnMouseDown(MouseDownEvent evt)
            {
                if (evt.button == 0)
                {
                    // Используем глобальную позицию мыши для точности
                    startMousePosition = evt.mousePosition;
                    startPanelPosition = new Vector2(resolvedStyle.left, resolvedStyle.top);
                    m_Header.CaptureMouse();
                }
            }
            
            m_Header.RegisterCallback<MouseDownEvent>(OnMouseDown);
            OnDetachFromPanelEvent += () => m_Header.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            
            void OnMouseMove(MouseMoveEvent evt)
            {
                if (m_Header.HasMouseCapture())
                {
                    // Рассчитываем разницу с использованием глобальной позиции мыши
                    var diff = evt.mousePosition - startMousePosition;
                    var nextPosition = startPanelPosition + diff;
                    m_Preferences.Position = nextPosition;
                    ApplyPreferences(m_Preferences);
                }
            }

            m_Header.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            OnDetachFromPanelEvent += () => m_Header.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            
            void OnMouseUp(MouseUpEvent evt)
            {
                if (evt.button == 0)
                {
                    m_Header.ReleaseMouse();
                    WritePreferences();
                }
            }

            m_Header.RegisterCallback<MouseUpEvent>(OnMouseUp);
            OnDetachFromPanelEvent += () => m_Header.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }
        
        public void AddContent(VisualElement content)
        {
            m_Content.Add(content);
        }
    }
}