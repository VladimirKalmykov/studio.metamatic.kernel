namespace Metamatic {
    using UnityEngine;

    [ExecuteAlways]
    public class Module: Element {
        public Vector3 position;
        public Quaternion rotation;
        public bool sticky = false;
        [Tooltip("Update position in editor every frame. Otherwise, update only on validate")]
        public bool editorLiveUpdate = true;

        void Start() {
            var parent = transform.parent;

            Stick();
        }

        public void Stick() {
            var parent = transform.parent;

            if (sticky && parent != null) {
                transform.localPosition = position;
                transform.localRotation = rotation;
            }
        }

        #if UNITY_EDITOR
        void Update() {
            if (sticky && editorLiveUpdate && !Application.isPlaying) {
                transform.localPosition = position;
                transform.localRotation = rotation;
            }
        }

        void OnValidate() {
            if (sticky) {
                transform.localPosition = position;
                transform.localRotation = rotation;
            }
        }
        #endif
    }
}