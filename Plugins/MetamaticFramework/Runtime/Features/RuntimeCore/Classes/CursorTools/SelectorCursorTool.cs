using System;
using Cysharp.Threading.Tasks;
using Metamatic.Effects;
using UnityEngine;

namespace Metamatic.Framework.CursorTools
{
    public class SelectorCursorTool : AsyncCursorTool
    {
        private Camera m_Camera;
        private bool m_IsPressed;
        private Collider2D m_ColliderUnderCursor;
        private RuntimeCore m_Core;

        public SelectorCursorTool() : base("Effects/Heal", "Heal")
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

        public override async UniTaskVoid OnActivate(RuntimeCore core, CancellationSignal signal)
        {
            m_Core = core;
            using var trackObjects = PlayerLoop.MicroStep(PlayerLoop.MicroSteps.Zeta, () =>
            {
                if (!m_IsPressed)
                {
                    if (m_Camera == null) return;
                    var ray = m_Camera.ScreenPointToRay(ScreenPosition);
                    var hit2d = Physics2D.Raycast(ray.origin, ray.direction, float.MaxValue);
                    if (hit2d.collider != null)
                    {
                        m_ColliderUnderCursor = hit2d.collider;
                        m_Core.SetCursorLabel(m_ColliderUnderCursor.gameObject.name);
                    }
                    else
                    {
                        m_ColliderUnderCursor = null;
                        m_Core.SetCursorLabel("");
                    }
                }
            });
            await signal;
        }

        public override void OnPress(Vector2 screenPosition)
        {
            m_IsPressed = true;
            if (m_Camera == null) return;
            /* Send a ray from the camera to the mouse position */
            var ray = m_Camera.ScreenPointToRay(screenPosition);
            var hit2d = Physics2D.Raycast(ray.origin, ray.direction, float.MaxValue);
            if (hit2d.collider != null)
            {
                m_Core.ToggleObjectSelection(hit2d.collider.gameObject);
            }
        }

        public override void OnRelease(Vector2 screenPosition)
        {
            m_IsPressed = false;
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