using System;
using Cysharp.Threading.Tasks;
using Metamatic.Effects;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic.Framework.CursorTools
{
    [MetamaticCursorTool("System/Select", "Select", "Sprites/Tools/Icons/Selector")]
    public class SelectorCursorTool : AsyncCursorTool
    {
        private Camera m_Camera;
        protected bool IsPressed;
        private Collider2D m_ColliderUnderCursor;
        protected DevToolsOverlay Overlay;
        private Label m_CursorTextElement;

        public SelectorCursorTool()
        {
            InitAsync();
        }

        protected override async UniTaskVoid OnInitAsync(Func<UniTask> ready, CancellationSignal signal)
        {
            var cameraService = await Service.Resolve<CameraService>(signal);
            
            
            
            using var subscribeToCamera = cameraService.SubscribeToMainCamera(cameraController =>
            {
                if (cameraController == null) return;
                m_Camera = cameraController.camera;
            });
            await signal;
            
        }

        public override async UniTaskVoid OnActivate(DevToolsOverlay overlay, CancellationSignal signal)
        {
            Overlay = overlay;
            /* Create cursor hint and it to the overlay root */
            m_CursorTextElement = new Label();
            m_CursorTextElement.AddToClassList("CursorText");
            m_CursorTextElement.style.display = DisplayStyle.None;
            Overlay.root.Add(m_CursorTextElement);
            
            using var trackObjects = PlayerLoop.MicroStep(PlayerLoop.MicroSteps.Zeta, () =>
            {
                if (Overlay.IsHoveringOverPanel.Value)
                {
                    m_ColliderUnderCursor = null;
                    m_CursorTextElement.style.display = DisplayStyle.None;
                    return;
                }
                if (!IsPressed)
                {
                    if (m_Camera == null) return;
                    var ray = m_Camera.ScreenPointToRay(ScreenPosition);
                    var hit2d = Physics2D.Raycast(ray.origin, ray.direction, float.MaxValue);
                    if (hit2d.collider != null)
                    {
                        m_ColliderUnderCursor = hit2d.collider;
                        m_CursorTextElement.style.display = DisplayStyle.Flex;
                        m_CursorTextElement.text = m_ColliderUnderCursor.gameObject.name;
                    }
                    else
                    {
                        m_ColliderUnderCursor = null;
                        m_CursorTextElement.style.display = DisplayStyle.None;
                    }
                }
                /* Convert screen position to UIDocument root element position */
                var localPosition = RuntimePanelUtils.ScreenToPanel(Overlay.root.panel, new Vector2(
                    ScreenPosition.x,
                    Screen.height - ScreenPosition.y    
                ));
                
                m_CursorTextElement.style.left = localPosition.x + 20;
                m_CursorTextElement.style.top = localPosition.y + 20;
            });
            await signal;
            Overlay.root.Remove(m_CursorTextElement);
        }

        public override void OnPress(Vector2 screenPosition, InputModifiers modifiers)
        {
            IsPressed = true;
            if (m_Camera == null || Overlay.IsHoveringOverPanel.Value) return;
            /* Send a ray from the camera to the mouse position */
            var ray = m_Camera.ScreenPointToRay(screenPosition);
            var hit2d = Physics2D.Raycast(ray.origin, ray.direction, float.MaxValue);
            if (hit2d.collider != null)
            {
                Overlay.core.ToggleObjectSelection(hit2d.collider.gameObject, modifiers.Shift);
            }
        }

        public override void OnRelease(Vector2 screenPosition)
        {
            IsPressed = false;
        }

        public override void OnMove(Vector2 screenPos)
        {
            
        }

        protected override void OnDispose()
        {
            throw new NotImplementedException();
        }
    }
}