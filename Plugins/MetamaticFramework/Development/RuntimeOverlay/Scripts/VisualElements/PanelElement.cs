

using System;
using Cysharp.Threading.Tasks;
using Metamatic.UIElements;
using Metamatic.UIToolKit;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic.Framework
{
    public class PanelElement: AsyncVisualElement
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
            public bool AutoHeight;
            public bool AutoWidth;
            
            public Constraints(UserPreferences defaultPreferences, Vector2 minSize, Vector2 maxSize, bool autoWidth = false, bool autoHeight = true)
            {
                DefaultPreferences = defaultPreferences;
                MinSize = minSize;
                MaxSize = maxSize;
                AutoWidth = autoWidth;
                AutoHeight = autoHeight;
            }
        }
        public readonly string PanelName;
        private VisualElement m_Header;
        private Label m_Title;
        private Button m_RightButton;
        private ScrollView m_Content;
        private VisualElement m_Resizer;
        private ObservableReadOnly<DevToolsOverlay> m_OverlayContext;
        private UserPreferences m_Preferences;
        private Constraints m_Constraints;
        private Observable<bool> m_IsResizing = new Observable<bool>(false);
        private Vector2 m_StartResizeMousePosition;
        private Vector2 m_StartResizeSize;
        
        public override VisualElement contentContainer => m_Content;
        
        public string title
        {
            get => m_Title.text;
            set => m_Title.text = value;
        }
        
        public PanelElement(): this("NewPanel", "New panel")
        {
        }
        public PanelElement(string name, string title, Constraints constraints = default, Action onClose = null)
        {
            AddToClassList(UIToolKitCore.DarkEnoughClassName);
            this.PanelName = name;
            m_Constraints = constraints;
            AddToClassList("MetamaticOverlay_Panel");
            m_Header = new VisualElement();
            m_Header.AddToClassList("MetamaticOverlay_PanelHeader");
            
            m_Title = new Label("Panel");
            m_Title.AddToClassList("MetamaticOverlay_PanelTitle");
            m_Title.text = title;
            m_Header.Add(m_Title);
            
            var spacer = new VisualElement();
            spacer.AddToClassList("MetamaticOverlay_PanelHeaderSpace");
            m_Header.Add(spacer);

            if (onClose != null)
            {
                m_RightButton = new Button(() => { });
                m_RightButton.AddToClassList("MetamaticOverlay_PanelHeaderRightButton");
                m_RightButton.style.backgroundImage = new StyleBackground(UIToolKitCore.closeIcon12X12);
                m_RightButton.clicked += onClose;
                m_Header.Add(m_RightButton);
            }

            m_Content = new ScrollView
            {
                name = "unity-content-container"
            };
            m_Content.AddToClassList("MetamaticOverlay_PanelContent");
            
            hierarchy.Add(m_Header);
            hierarchy.Add(m_Content);
            
            // Add resizer
            var resizer = m_Resizer = new VisualElement();
            resizer.AddToClassList("MetamaticOverlay_PanelResizer");
            hierarchy.Add(resizer);
        }
        
        void EnterHandler(MouseEnterEvent evt)
        {
            m_OverlayContext.Value.AddHoveredPanel(this);
        }
        
        void LeaveHandler(MouseLeaveEvent evt)
        {
            m_OverlayContext.Value.RemoveHoveredPanel(this);
        }
        
        
        private void OnRootGeometryChanged(GeometryChangedEvent evt)
        {
            ApplyPreferences(m_Preferences);
        }

        public override async UniTask OnAttachedAsync(Signal detachSignal)
        {
            m_OverlayContext = this.GetContext<DevToolsOverlay>();

            if (m_OverlayContext == null) return;
            
            /* Load preferences */
            m_Preferences = m_OverlayContext.Value.GetPanelPreferences(PanelName, m_Constraints.DefaultPreferences);
            m_OverlayContext.Value.uiDocument.rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            
            /* Detect mouse enter and leave */
            RegisterCallback<MouseEnterEvent>(EnterHandler);
            RegisterCallback<MouseLeaveEvent>(LeaveHandler);
            ApplyPreferences(m_Preferences);
            InitDraggable(detachSignal).Forget();
            InitResizable(detachSignal).Forget();
            
            await detachSignal;
            UnregisterCallback<MouseEnterEvent>(EnterHandler);
            UnregisterCallback<MouseLeaveEvent>(LeaveHandler);
            m_OverlayContext.Value.uiDocument.rootVisualElement.UnregisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
        }

        private void ApplyPreferences(UserPreferences preferences)
        {
            var finalPosition = preferences.Position;
            var constrainedSize = preferences.Size;
            /* Apply constraints */
            if (m_Constraints.MinSize != Vector2.zero)
            {
                constrainedSize = Vector2.Max(constrainedSize, m_Constraints.MinSize);
            }
            if (m_Constraints.MaxSize.x != 0)
            {
                constrainedSize.x = Mathf.Min(constrainedSize.x, m_Constraints.MaxSize.x);
            }
            if (m_Constraints.MaxSize.y != 0)
            {
                constrainedSize.y = Mathf.Min(constrainedSize.y, m_Constraints.MaxSize.y);
            }
            /* Apply constraints relative screen size. Panel should not be outside of the screen () */
            var overlay = m_OverlayContext.Value;
            finalPosition = Vector2.Max(finalPosition, Vector2.zero);
            var uiDocumentWidth = overlay.uiDocument.rootVisualElement.layout.width;
            var uiDocumentHeight = overlay.uiDocument.rootVisualElement.layout.height;
            if (!float.IsNaN(uiDocumentWidth) && !float.IsNaN(uiDocumentHeight))
            {
                finalPosition = new Vector2(
                    Mathf.Min(finalPosition.x, uiDocumentWidth - constrainedSize.x),
                    Mathf.Min(finalPosition.y, uiDocumentHeight - constrainedSize.y)
                );
            }
            
            /* Apply preferences */
            style.left = finalPosition.x;
            style.top = finalPosition.y;
            style.width = m_Constraints.AutoWidth ? new StyleLength(StyleKeyword.Auto) : preferences.Size.x;
            style.height = m_Constraints.AutoHeight ? new StyleLength(StyleKeyword.Auto) : preferences.Size.y;
            style.maxWidth = m_Constraints.MaxSize.x == 0 ? StyleKeyword.None : m_Constraints.MaxSize.x;
            style.maxHeight = m_Constraints.MaxSize.y == 0 ? StyleKeyword.None : m_Constraints.MaxSize.y;
            style.minHeight = m_Constraints.MinSize.y == 0 ? StyleKeyword.None : m_Constraints.MinSize.y;
            style.minWidth = m_Constraints.MinSize.x == 0 ? StyleKeyword.None : m_Constraints.MinSize.x;
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

        private async UniTaskVoid InitDraggable(Signal signal)
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
            
            void OnMouseUp(MouseUpEvent evt)
            {
                if (evt.button == 0)
                {
                    m_Header.ReleaseMouse();
                    WritePreferences();
                }
            }

            m_Header.RegisterCallback<MouseUpEvent>(OnMouseUp);
            
            await signal;
            m_Header.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            m_Header.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            m_Header.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }

        private async UniTaskVoid InitResizable(Signal signal)
        {
            // Listen for mouse down on resizer
            m_Resizer.RegisterCallback<MouseDownEvent>(OnMouseDownOnResizer);
            m_Resizer.RegisterCallback<MouseMoveEvent>(OnMouseMoveOnResizer);
            m_Resizer.RegisterCallback<MouseUpEvent>(OnMouseUpOnResizer);
            await signal;
            m_Resizer.UnregisterCallback<MouseDownEvent>(OnMouseDownOnResizer);
            m_Resizer.UnregisterCallback<MouseMoveEvent>(OnMouseMoveOnResizer);
            m_Resizer.UnregisterCallback<MouseUpEvent>(OnMouseUpOnResizer);
        }
        
        private void OnMouseDownOnResizer(MouseDownEvent evt)
        {
            if (evt.button == 0)
            {
                m_StartResizeMousePosition = evt.mousePosition;
                m_StartResizeSize = new Vector2(resolvedStyle.width, resolvedStyle.height);
                m_IsResizing.Value = true;
                m_Resizer.CaptureMouse();
            }
        }
        
        private void OnMouseUpOnResizer(MouseUpEvent evt)
        {
            if (evt.button == 0)
            {
                m_IsResizing.Value = false;
                m_Resizer.ReleaseMouse();
                WritePreferences();
            }
        }
        private void OnMouseMoveOnResizer(MouseMoveEvent evt)
        {
            if (m_IsResizing.Value)
            {
                var diff = evt.mousePosition - m_StartResizeMousePosition;
                var nextSize = m_StartResizeSize + diff;
                
                /* Fix next size according constraints */
                if (m_Constraints.AutoWidth)
                {
                    nextSize.x = m_StartResizeSize.x;
                }
                if (m_Constraints.AutoHeight)
                {
                    nextSize.y = m_StartResizeSize.y;
                }
                if (m_Constraints.MinSize != Vector2.zero)
                {
                    nextSize = Vector2.Max(nextSize, m_Constraints.MinSize);
                }
                if (m_Constraints.MaxSize.x != 0)
                {
                    nextSize.x = Mathf.Min(nextSize.x, m_Constraints.MaxSize.x);
                }
                if (m_Constraints.MaxSize.y != 0)
                {
                    nextSize.y = Mathf.Min(nextSize.y, m_Constraints.MaxSize.y);
                }
                
                m_Preferences.Size = nextSize;
                ApplyPreferences(m_Preferences);
            }
        }
        
        public void AddContent(VisualElement content)
        {
            m_Content.Add(content);
        }
    }
}