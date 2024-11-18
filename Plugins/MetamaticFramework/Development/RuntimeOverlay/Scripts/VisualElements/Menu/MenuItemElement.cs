using System;
using Cysharp.Threading.Tasks;
using Metamatic.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic.Framework
{
    public class MenuItemElement : AsyncVisualElement, IDisposable
    {
        #region Constants
        private const string KCheckedIconPath = "Sprites/Icons/Checked";
        #endregion
        
        #region Static
        private static Texture2D s_CheckedIconTexture;
        public static Texture2D CheckedIconTexture
        {
            get
            {
                if (s_CheckedIconTexture == null)
                {
                    s_CheckedIconTexture = Resources.Load<Texture2D>(KCheckedIconPath);
                }

                return s_CheckedIconTexture;
            }
        }
        #endregion
        
        private MenuItem m_Source;
        private Label m_Label;
        private Image m_CheckedIcon;
        private Observable<bool> m_Hovered = new Observable<bool>();
        private event Action OnClickedEvent;

        public event Action OnMouseEnterEvent;
        public event Action OnMouseLeaveEvent;

        public string text
        {
            get => m_Label.text;
            set => m_Label.text = value;
        }

        public MenuItemElement(MenuItem source)
        {
            m_Source = source;
            
            // Add label
            AddToClassList("MenuItem");
            m_Label = new Label();
            Add(m_Label);

            // Add checked icon
            m_CheckedIcon = new Image();
            m_CheckedIcon.image = CheckedIconTexture;
            m_CheckedIcon.AddToClassList("Icon");
            m_CheckedIcon.style.display = DisplayStyle.None;
            
            Add(m_CheckedIcon);
            
            ApplySource();
        }

        #region AsyncVisualElement

        public override async UniTask OnAttachedAsync(CancellationSignal detachSignal)
        {
            // Subscribe to the click event
            RegisterCallback<ClickEvent>(OnClick);
            // Subscribe to the mouse enter event
            RegisterCallback<MouseEnterEvent>(OnLabelMouseEnter);
            // Subscribe to the mouse leave event
            RegisterCallback<MouseLeaveEvent>(OnLabelMouseLeave);
            // Watch the source for changes
            using var watchSource = m_Source.Subscribe(ApplySource);
            // Watch the source for submenu add/remove events
            using var watchSubmenu = m_Source.WatchSubmenuLifecycle(WatchHoveredForShowingPanel);
            // Watch observable bool state
            using var watchObservableBoolState = m_Source.BoolState?.Watch(OnObservableBoolStateChanged);
            await detachSignal;

            // Unsubscribe from the click event
            UnregisterCallback<ClickEvent>(OnClick);
            // Unsubscribe from the mouse enter event
            UnregisterCallback<MouseEnterEvent>(OnLabelMouseEnter);
            // Unsubscribe from the mouse leave event
            UnregisterCallback<MouseLeaveEvent>(OnLabelMouseLeave);
        }

        #endregion
        
        private void OnObservableBoolStateChanged(bool value)
        {
            m_CheckedIcon.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ApplySource()
        {
            m_Label.text = m_Source.name;
        }

        private void OnClick(ClickEvent evt)
        {
            m_Source.Trigger?.Invoke();
            OnClickedEvent?.Invoke();
        }

        private void OnLabelMouseEnter(MouseEnterEvent evt)
        {
            OnMouseEnterEvent?.Invoke();
            m_Hovered.SetUniqValue(true);
        }

        private void OnLabelMouseLeave(MouseLeaveEvent evt)
        {
            OnMouseLeaveEvent?.Invoke();
            m_Hovered.SetUniqValue(false);
        }

        public DisposableAction WatchHoveredLifecycle(Func<bool, CancellationSignal, UniTaskVoid> onHoveredChanged)
        {
            return m_Hovered.WatchLifecycle(onHoveredChanged);
        }

        public DisposableAction SubscribeToClick(Action onClicked)
        {
            OnClickedEvent += onClicked;
            return new DisposableAction(() => OnClickedEvent -= onClicked);
        }

        public void Dispose()
        {
            var menu = this.FindParent<MenuElement>();
            if (menu != null)
            {
                menu.RemoveMenuItem(this);
            }
        }

        public DisposableAction WatchMouseHoverChange(Action<bool> action, bool skipInitial = false)
        {
            void EnterHandler()
            {
                action(true);
            }

            void LeaveHandler()
            {
                action(false);
            }

            OnMouseEnterEvent += EnterHandler;
            OnMouseLeaveEvent += LeaveHandler;

            if (!skipInitial)
            {
                action(m_Hovered.Value);
            }


            return new DisposableAction(() =>
            {
                OnMouseEnterEvent -= EnterHandler;
                OnMouseLeaveEvent -= LeaveHandler;
            });
        }

        private async UniTaskVoid WatchHoveredForShowingPanel(Menu submenu, CancellationSignal cancellationSignal)
        {
            if (submenu != null)
            {
                // Hover observer for both the menu item and the menu panel
                var parentMenuPanel = this.FindParent<MenuPanel>();
                async UniTaskVoid HandleCombinedHover(bool isHovered, CancellationSignal cancellationSignal)
                {
                    if (isHovered)
                    {
                        AddToClassList("Hover");
                        // Add menu panel
                        var menuPanel = new MenuPanel(this, parentMenuPanel == null ? MenuPanel.Placement.Bottom : MenuPanel.Placement.Right);
                        var subMenuElement = new MenuElement(MenuElement.Orientation.Vertical, submenu);
                        menuPanel.Add(subMenuElement);
                        var overlayRoot = this.GetContext<DevToolsOverlay>().value.root;

                        Add(menuPanel);
                        await cancellationSignal;
                        Remove(menuPanel);
                    }
                    else
                    {
                        RemoveFromClassList("Hover");
                    }
                }

                using var watchHovered = m_Hovered.WatchLifecycle(HandleCombinedHover);
                await cancellationSignal;
            }
        }
    }
}