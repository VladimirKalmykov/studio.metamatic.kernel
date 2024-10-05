using System;
using Cysharp.Threading.Tasks;
using UnityEngine.UIElements;

namespace Metamatic.Framework
{
    public class MenuElement: AsyncVisualElement
    {
        public enum Orientation
        {
            Vertical,
            Horizontal
        }
        private readonly Menu m_Source;
        public event Action MouseEnterEvent;
        public event Action MouseLeaveEvent;
        public event Action MouseClickEvent;
        
        private CancellationSignalSource m_MouseEnterCancellationSignalSource;
        public Orientation orientation { get; private set; }
        private readonly Observable<bool> m_IsActive = new();
        
        public MenuElement(Orientation orientation, Menu source)
        {
            this.orientation = orientation;
            m_Source = source;
            AddToClassList("Menu");
            if (orientation == Orientation.Vertical)
            {
                AddToClassList("Vertical");
            }
            else
            {
                AddToClassList("Horizontal");
            }
        }
        
        #region AsyncVisualElement
        public override async UniTask OnAttachedAsync(CancellationSignal detachSignal)
        {
            /* Listen for mouse enter and leave events */
            RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            
            /* Listen for click events */
            RegisterCallback<ClickEvent>(OnMouseClick);
            
            using var watchSource = m_Source.WatchEachItemLifecycle(OnItemLifecycleSwitch);
            await detachSignal;
            
            m_MouseEnterCancellationSignalSource.TryCancel();
            
            /* Unsubscribe from mouse enter and leave events */
            MouseEnterEvent = null;
            MouseLeaveEvent = null;
            MouseClickEvent = null;
            
            /* Unsubscribe from click events */
            UnregisterCallback<ClickEvent>(OnMouseClick);
            UnregisterCallback<MouseEnterEvent>(OnMouseEnter);
            UnregisterCallback<MouseLeaveEvent>(OnMouseLeave);
        }
        #endregion

        public void SetActive(bool active)
        {
            m_IsActive.SetUniqValue(active);
        }
        
        private async UniTaskVoid OnItemLifecycleSwitch(MenuItem menuItem, CancellationSignal detachSignal)
        {
            var item = AddMenuItem(menuItem);
            await detachSignal;
            RemoveMenuItem(item);
        }
        
        public MenuItemElement AddMenuItem(MenuItem menuItem)
        {
            var menuItemElement = new MenuItemElement(menuItem);
            Add(menuItemElement);
            return menuItemElement;
        }
        
        public bool RemoveMenuItem(MenuItemElement menuItem)
        {
            if (menuItem.parent == this)
            {
                Remove(menuItem);
                return true;
            }

            return false;
        }
        
        public DisposableAction WatchMouseEnterLifecycle(Func<CancellationSignal, UniTaskVoid> onMouseEnter)
        {
            void Handler()
            {
                onMouseEnter(m_MouseEnterCancellationSignalSource.Signal).Forget();
            }
            
            MouseEnterEvent += Handler;
            return new DisposableAction(() => MouseEnterEvent -= Handler);
        }

        public DisposableAction WatchMouseHoverChange(Action<bool> action)
        {
            void EnterHandler()
            {
                action(true);
            }
            
            void LeaveHandler()
            {
                action(false);
            }
            
            MouseEnterEvent += EnterHandler;
            MouseLeaveEvent += LeaveHandler;
            
            return new DisposableAction(() =>
            {
                MouseEnterEvent -= EnterHandler;
                MouseLeaveEvent -= LeaveHandler;
            });
        }

        private void OnMouseEnter(MouseEnterEvent evt)
        {
            m_MouseEnterCancellationSignalSource.Dispose();
            m_MouseEnterCancellationSignalSource = CancellationSignalSource.Create();
            MouseEnterEvent?.Invoke();
        }

        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            m_MouseEnterCancellationSignalSource.TryCancel();
            MouseLeaveEvent?.Invoke();
        }
        
        private void OnMouseClick(ClickEvent evt)
        {
            MouseClickEvent?.Invoke();
        }
    }
}