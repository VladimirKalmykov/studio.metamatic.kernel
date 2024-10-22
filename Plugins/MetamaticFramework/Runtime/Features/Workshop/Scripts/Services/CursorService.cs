using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

namespace Metamatic.Framework.Workshop
{
    public class CursorService: Service
    {
        public readonly struct CursorConfig
        {
            public readonly Texture2D CursorTexture;
            public readonly Vector2 Hotspot;
            public readonly bool Visible;
        }
        [SerializeField] private Texture2D m_CursorTexture;
        [SerializeField] private Vector2 m_Hotspot = Vector2.zero;
        [SerializeField] private bool m_Visible = true;
        
        private readonly List<(string, CursorConfig)> m_Cursors = new();
        
        protected override async UniTask RunService(Ready ready, CancellationSignal signal)
        {
            ResetCursorToDefaults();

            await signal;
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
        /// Скрыть курсор и заблокировать его
        /// </summary>
        private static void HideCursor()
        {
            Cursor.visible = false;  // Скрыть курсор
            Cursor.lockState = CursorLockMode.Locked; // Заблокировать курсор в центре экрана
        }

        /// <summary>
        /// Показать курсор и разблокировать его
        /// </summary>
        private static void ShowCursor()
        {
            Cursor.visible = true;  // Показать курсор
            Cursor.lockState = CursorLockMode.None; // Разблокировать курсор
        }
        
        public void ResetCursorToDefaults()
        {
            if (m_CursorTexture != null)
            {
                Cursor.SetCursor(m_CursorTexture, m_Hotspot, CursorMode.Auto);
            }
            if (!m_Visible)
            {
                HideCursor();
            } else {
                ShowCursor();
            }
        }

        public static void ResetCursor()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}