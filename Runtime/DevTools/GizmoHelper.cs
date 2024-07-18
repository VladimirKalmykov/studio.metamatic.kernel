using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Metamatic.DevTools
{
    public class GizmoHelper : MonoBehaviour
    {
        #if UNITY_EDITOR
        [LabelText("Title")]
        public string defaultTitle = "";
        
        public Color color = new Color(0f, 1f, 0.01f);
        public Color rightColor = new Color(1f, 0f, 0.06f);
        public float arrowSize = 1f;

        protected bool gizmo_DrawCoords = true;

        public string title
        {
            get
            {
                return defaultTitle == "" ? name : defaultTitle;
            }
        }

        protected static Mesh arrowMesh;
        #endif

        private void Start()
        {
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

            if (gizmo_DrawCoords)
            {
                var rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.matrix = rotationMatrix;
                Gizmos.color = color;
                Gizmos.DrawMesh(arrowMesh, Vector3.zero, Quaternion.Euler(0f, 0f, 90f),
                    Vector3.one * 0.333f * arrowSize * HandleUtility.GetHandleSize(transform.position));

                Gizmos.color = rightColor;
                Gizmos.DrawMesh(arrowMesh, Vector3.zero, Quaternion.identity,
                    Vector3.one * 0.333f * arrowSize * HandleUtility.GetHandleSize(transform.position));
            }
        }
#endif
    }
}