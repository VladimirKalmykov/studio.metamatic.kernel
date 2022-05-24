using System;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Metamatic.DevTools
{
    public class Gizmo : MonoBehaviour
    {
        #if UNITY_EDITOR
        [LabelText("Title")]
        public string defaultTitle = "";
        
        public Color color = Color.magenta;
        public Color rightColor = new Color(1f, 0f, 0.06f);
        public Vector3 arrowSize = Vector3.one;

        public string title
        {
            get
            {
                return defaultTitle == "" ? name : defaultTitle;
            }
        }

        private static Mesh arrowMesh;
        #endif

        private void Start()
        {
            Debug.Log("Start!");
            #if !UNITY_EDITOR
            enabled = false;
            #endif
        }
        
#if UNITY_EDITOR
        protected void OnDrawGizmos()
        {
            if (arrowMesh == null)
            {
                arrowMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Packages/studio.metamatic.kernel/Editor/DevTools/Assets/Meshes/arrow.asset");
            }
            var rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = rotationMatrix;
            Gizmos.color = color;
            Gizmos.DrawMesh(arrowMesh, Vector3.zero, Quaternion.Euler(0f, 0f, 90f), arrowSize);
            
            Gizmos.color = rightColor;
            Gizmos.DrawMesh(arrowMesh, Vector3.zero, Quaternion.identity, arrowSize);
        }
#endif
    }
}