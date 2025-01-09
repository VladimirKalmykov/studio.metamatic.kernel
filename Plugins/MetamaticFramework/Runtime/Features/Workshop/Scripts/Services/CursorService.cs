using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Framework.Workshop
{
    public class CursorService: Service
    {
        public readonly struct CursorConfig
        {
            public readonly Texture2D CursorTexture;
            public readonly Vector2 Hotspot;
            public readonly bool Visible;
            
            public CursorConfig(bool visible, Texture2D cursorTexture = null, Vector2 hotspot = default)
            {
                CursorTexture = cursorTexture;
                Hotspot = hotspot;
                Visible = visible;
            }
        }
        [SerializeField] private Texture2D m_CursorTexture;
        [SerializeField] private Vector2 m_Hotspot = Vector2.zero;
        [SerializeField] private bool m_Visible = true;
        
        private readonly List<(string, CursorConfig)> m_Cursors = new();
        
        protected override async UniTask RunService(Ready ready, Signal signal)
        {
            ResetCursorToDefaults();
            await ready();
            ResetCursor();
        }
        
        /// <summary>
        /// Set cursor to the specified cursorConfig
        /// This method returns a DisposableAction that should be disposed when the cursor should be reset to the previous state
        /// </summary>
        /// <param name="cursorConfig"></param>
        /// <returns></returns>
        public DisposableAction SetCursor(CursorConfig cursorConfig)
        {
            var id = Guid.NewGuid().ToString();
            m_Cursors.Add((id, cursorConfig));
            OnCursorChanged(cursorConfig);
            return new DisposableAction(() =>
            {
                m_Cursors.RemoveAll(x => x.Item1 == id);
                if (m_Cursors.Count > 0)
                {
                    OnCursorChanged(m_Cursors[^1].Item2);
                }
                else
                {
                    ResetCursorToDefaults();
                }
            });
        }
        
        private void OnCursorVisibilityChanged(bool visible)
        {
            if (visible)
            {
                ShowCursor();
            }
            else
            {
                HideCursor();
            }
        }
        
        private void OnCursorChanged(CursorConfig cursorConfig)
        {
            m_CursorTexture = cursorConfig.CursorTexture;
            m_Hotspot = cursorConfig.Hotspot;
            m_Visible = cursorConfig.Visible;
            Cursor.SetCursor(m_CursorTexture, m_Hotspot, CursorMode.Auto);
            if (!m_Visible)
            {
                HideCursor();
            } else {
                ShowCursor();
            }
        }
        
        /// <summary>
        /// Hide and lock cursor. 
        /// </summary>
        private static void HideCursor()
        {
            Cursor.visible = false;  // Скрыть курсор
            Cursor.lockState = CursorLockMode.Locked; // Заблокировать курсор в центре экрана
        }

        /// <summary>
        /// Show and unlock cursor. 
        /// </summary>
        private static void ShowCursor()
        {
            Cursor.visible = true;  // Показать курсор
            Cursor.lockState = CursorLockMode.None; // Разблокировать курсор
        }
        
        public void ResetCursorToDefaults()
        {
            m_Cursors.Clear();
            var defaultCursorConfig = new CursorConfig(true, m_CursorTexture, m_Hotspot);
            SetCursor(defaultCursorConfig);
        }

        public static void ResetCursor()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}