using System.Collections.Generic;
using Metamatic;
using Metamatic.DevTools;
using UnityEngine;

namespace DevTools
{
    public class GlobalGizmosBehaviour : MonoBehaviour
    {
        private List<GlobalGizmo> m_Gizmos = new ();
        
        public DisposableAction AddGizmo(System.Action renderer)
        {
            var gizmo = new GlobalGizmo(renderer);
            m_Gizmos.Add(gizmo);
            return new DisposableAction(() =>
            {
                m_Gizmos.Remove(gizmo);
            });
        }
        
        #if UNITY_EDITOR
        public void OnDrawGizmos()
        {
            foreach (var gizmo in m_Gizmos)
            {
                gizmo.Render();
            }
        }
        #endif
    }
}