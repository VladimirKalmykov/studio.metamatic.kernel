using System;

namespace Metamatic.UI
{
    public abstract class UIOverlayWidgetLegacy: WidgetLegacy
    {
        protected bool m_OverlayAcquired;
        private UIOverlay m_Overlay;
        public UIOverlay overlay
        {
            get
            {
                if (m_Overlay is null)
                {
                    m_Overlay = GetComponentInParent<UIOverlay>(true);
                }
                return m_Overlay;
            }
        }
        
        protected override void OnEnable()
        {
            base.OnEnable();
            if (overlay == null)
            {
                throw new Exception("UIOverlay not found in parent hierarchy");
            }
        }
        
        protected void AcquireOverlay()
        {
            if (m_OverlayAcquired)
            {
                return;
            }
            overlay.Acquire();
            m_OverlayAcquired = true;
        }
        
        protected void ReleaseOverlay()
        {
            if (!m_OverlayAcquired)
            {
                return;
            }
            overlay.Release();
            m_OverlayAcquired = false;
        }

        protected override void BeforeShow()
        {
            AcquireOverlay();
        }
        
        protected override void AfterHide()
        {
            ReleaseOverlay();
        }
    }
}