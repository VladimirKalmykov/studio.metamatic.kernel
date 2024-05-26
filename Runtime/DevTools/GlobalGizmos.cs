using System;
using DevTools;
using UnityEngine;

namespace Metamatic.DevTools
{
    public class GlobalGizmo: IDisposable
    {
        private readonly System.Action m_GizmoRenderer;
        public GlobalGizmo(System.Action renderer)
        {
            m_GizmoRenderer = renderer;
        }

        public void Render()
        {
            m_GizmoRenderer();
        }

        public void Dispose()
        {
            
        }
    }
    
    public class GlobalGizmos
    {
        private static GlobalGizmosBehaviour m_Instance;
        public static GlobalGizmosBehaviour instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new GameObject("__GIZMOS__").AddComponent<GlobalGizmosBehaviour>();
                }

                return m_Instance;
            }
        }
        
        public static DisposableAction AddGizmo(System.Action renderer)
        {
            return instance.AddGizmo(renderer);
        }
    }
}