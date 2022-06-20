using System;
using UnityEditor;

namespace Metamatic {
    using UnityEngine;
    using Sirenix.OdinInspector;

    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class Element: PluggableMonoBehaviour {
        [BoxGroup("Element"), ShowInInspector, ReadOnly]
        public Element parentElement { get; private set; }
        public Configurator configurator { get; private set; }
        [BoxGroup("Element")]
        [ShowInInspector]
        public string absoluteName {
            get {
                return parentElement == null ? name : $"{parentElement.absoluteName}.{name}";
            }
        }
        [BoxGroup("Element")]
        [ShowInInspector]
        public string relativeName {
            get {
                if (parentElement == null) {
                    return name;
                }
                return parentElement.configurator == configurator ? $"{parentElement.relativeName}.{name}" : name;
            }
        }

        [BoxGroup("Element")]
        [Button]
        void CopyAbsoluteName() {
            GUIUtility.systemCopyBuffer = absoluteName;
        }
        [BoxGroup("Element")]
        [Button]
        void CopyRelativeName() {
            GUIUtility.systemCopyBuffer = relativeName;
        }
        void Awake() {
            parentElement = transform.parent != null ? transform.parent.GetComponentInParent<Element>() : null;
            configurator = transform.parent != null ? transform.parent.GetComponentInParent<Configurator>() : null;
        }

        void OnTransformParentChanged() {
            parentElement = transform.parent != null ? transform.parent.GetComponentInParent<Element>() : null;
            configurator = transform.parent != null ? transform.parent.GetComponentInParent<Configurator>() : null;
        }
        
        #if UNITY_EDITOR
        private static Mesh arrowMesh;
        private void OnDrawGizmosSelected()
        {
            if (arrowMesh == null)
            {
                arrowMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Packages/studio.metamatic.kernel/Editor/DevTools/Assets/Meshes/directionalArrow.asset");
            }
            var rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = rotationMatrix;
            Gizmos.color = new Color(1f, 0.88f, 0f);
            Gizmos.DrawMesh(arrowMesh, Vector3.zero, Quaternion.Euler(0f, 0f, -90f), Vector3.one * HandleUtility.GetHandleSize(transform.position));
            
            Gizmos.color = new Color(1f, 0f, 0.06f);
            Gizmos.DrawMesh(arrowMesh, Vector3.zero, Quaternion.identity, Vector3.one * HandleUtility.GetHandleSize(transform.position));
        }
#endif
    }
}