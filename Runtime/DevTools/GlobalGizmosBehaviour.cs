using System;
using System.Collections.Generic;
using Metamatic;
using Metamatic.DevTools;
using UnityEngine;

namespace DevTools
{
    public class GlobalGizmosBehaviour : MonoBehaviour
    {
        private List<GlobalGizmo> m_Gizmos = new ();
        private Dictionary<string, GlobalGizmo> m_MappedGizmos = new ();
        public event Action OnDrawGizmosEvent;
        
        public DisposableAction AddGizmo(System.Action renderer)
        {
            var gizmo = new GlobalGizmo(renderer);
            m_Gizmos.Add(gizmo);
            return new DisposableAction(() =>
            {
                m_Gizmos.Remove(gizmo);
            });
        }
        
        public void AddGizmo(string key, System.Action renderer)
        {
            if (m_MappedGizmos.TryGetValue(key, out var gizmo))
            {
                gizmo.renderer = renderer;
            }
            else
            {
                gizmo = new GlobalGizmo(renderer);
                m_MappedGizmos.Add(key, gizmo);
            }
        }
        
        public void RemoveGizmo(string key)
        {
            if (m_MappedGizmos.ContainsKey(key))
            {
                m_MappedGizmos.Remove(key);
            }
        }
        
        public void Once(System.Action renderer)
        {
            #if UNITY_EDITOR
            OnDrawGizmosEvent += renderer;
            #endif
        }
        
        #if UNITY_EDITOR
        public void OnDrawGizmos()
        {
            OnDrawGizmosEvent?.Invoke();
            OnDrawGizmosEvent = null;
            foreach (var gizmo in m_Gizmos)
            {
                gizmo.Render();
            }
            foreach (var gizmo in m_MappedGizmos.Values)
            {
                gizmo.Render();
            }
        }
        #endif
    }
}