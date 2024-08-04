using System;
using DevTools;
using UnityEngine;

namespace Metamatic.DevTools
{
    public class GlobalGizmo: IDisposable
    {
        public System.Action renderer;
        public GlobalGizmo(System.Action renderer)
        {
            this.renderer = renderer;
        }

        public void Render()
        {
            renderer();
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
        
        public static void Once(System.Action renderer)
        {
            instance.Once(renderer);
        }
        
        public static void AddGizmo(string key, System.Action renderer)
        {
            instance.AddGizmo(key, renderer);
        }
        
        public static void RemoveGizmo(string key)
        {
            instance.RemoveGizmo(key);
        }
    }
}