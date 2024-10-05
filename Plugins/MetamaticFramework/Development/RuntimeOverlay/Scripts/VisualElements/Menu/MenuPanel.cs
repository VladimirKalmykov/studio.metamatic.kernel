using System;
using Cysharp.Threading.Tasks;
using UnityEngine.UIElements;

namespace Metamatic.Framework
{
    public class MenuPanel: AsyncVisualElement
    {
        public enum Placement
        {
            Top,
            Right,
            Bottom,
            Left,
            Auto
        }

        public event Action MouseEnterEvent;
        public event Action MouseLeaveEvent;
        public bool IsMouseHovered { get; private set; } = false;
        
        public MenuPanel(VisualElement belongTo, Placement placement = Placement.Bottom)
        {
            AddToClassList("MenuPanel");
            
            Show(belongTo, placement);
        }

        public void Show(VisualElement belongTo, Placement placement = Placement.Bottom)
        {
            style.display = DisplayStyle.Flex;
            switch (placement)
            {
                case Placement.Top:
                    SetTopPlacement(belongTo);
                    break;
                case Placement.Right:
                    SetRightPlacement(belongTo);
                    break;
                case Placement.Bottom:
                    SetBottomPlacement(belongTo);
                    break;
                case Placement.Left:
                    SetLeftPlacement(belongTo);
                    break;
            }
        }
        
        public void Hide()
        {
            style.display = DisplayStyle.None;
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
            
            MouseEnterEvent += EnterHandler;
            MouseLeaveEvent += LeaveHandler;

            if (!skipInitial)
            {
                action(IsMouseHovered);
            }
            
            return new DisposableAction(() =>
            {
                MouseEnterEvent -= EnterHandler;
                MouseLeaveEvent -= LeaveHandler;
            });
        }
        
        private void SetTopPlacement(VisualElement belongTo)
        {
            style.left = belongTo.layout.x;
            style.top = belongTo.layout.y - layout.height;
        }
        
        private void SetRightPlacement(VisualElement belongTo)
        {
            style.left = belongTo.layout.width;
            style.top = -5;
        }
        
        private void SetBottomPlacement(VisualElement belongTo)
        {
            style.left = 0f;
            style.top = belongTo.layout.height;
        }
        
        private void SetLeftPlacement(VisualElement belongTo)
        {
            style.left = belongTo.layout.x - layout.width;
            style.top = belongTo.layout.y;
        }
        
        private void OnMouseEnter(MouseEnterEvent evt)
        {
            IsMouseHovered = true;
            MouseEnterEvent?.Invoke();
        }
        
        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            IsMouseHovered = false;
            MouseLeaveEvent?.Invoke();
        }

        public override async UniTask OnAttachedAsync(CancellationSignal detachSignal)
        {
            /* Listen for mouse enter and leave events */
            RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            await detachSignal;
            
            /* Unsubscribe from mouse enter and leave events */
            UnregisterCallback<MouseEnterEvent>(OnMouseEnter);
            UnregisterCallback<MouseLeaveEvent>(OnMouseLeave);
        }
    }
}