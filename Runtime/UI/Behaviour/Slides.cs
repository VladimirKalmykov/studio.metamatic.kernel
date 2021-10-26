namespace Metamatic.UI {
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Sirenix.OdinInspector;

    public class Slides : MonoBehaviour
    {
        public enum Orientation {
            HORIZONTAL,
            VERTICAL
        }
        #region Fields
        public Orientation orientation = Orientation.HORIZONTAL;
        public Vector2 positionRatio;
        
        [ShowInInspector, ReadOnly]
        #endregion

        #region Public properties
        public Vector2 size {
            get; private set;
        }
        public int count {
            get {
                return cachedChildren.Length;
            }
        }
        #endregion

        #region Private properties
        RectTransform rect;
        Transform[] cachedChildren = new Transform[0];
        int currentSlide = 0;
        #endregion
        void Awake() {
            rect = (RectTransform)transform;
        }
        void Start() {
            NormalizeChildren();
        }
        void Update() {
            size = new Vector2(rect.rect.width, rect.rect.height);

            rect.offsetMin = positionRatio * size;
            rect.offsetMax = positionRatio * size;
        }
        void FixedUpdate() {
            if (GetChildrenHaveChanged()) {
                NormalizeChildren();
            }
        }

        /// <summary>
        /// Checks if children have changed
        /// </summary>
        /// <returns></returns>
        bool GetChildrenHaveChanged() {
            if (cachedChildren.Length != transform.childCount) {
                return true; 
            }
            for (int i = 0; i < cachedChildren.Length; i++) {
                if (cachedChildren[i] != transform.GetChild(i)) {
                    return true;
                }
            }
            return false;
        }
        void NormalizeChildren() {
            int index = 0;
            cachedChildren = new Transform[transform.childCount];
            foreach(RectTransform child in transform) {
                child.anchoredPosition = Vector2.zero;
                child.sizeDelta = Vector2.zero;
                if (orientation == Orientation.HORIZONTAL) {
                    child.anchorMin = new Vector2(index, 0);
                    child.anchorMax = new Vector2(index + 1, 1);
                } else {
                    child.anchorMin = new Vector2(0, index);
                    child.anchorMax = new Vector2(1, index + 1);
                }
                child.pivot = Vector2.zero;
                cachedChildren[index] = child as Transform;
                index++;
            }
        }

        #if UNITY_EDITOR
        void OnValidate() {
            NormalizeChildren();
        }
        #endif
    }
}