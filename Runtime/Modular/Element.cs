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
    }
}