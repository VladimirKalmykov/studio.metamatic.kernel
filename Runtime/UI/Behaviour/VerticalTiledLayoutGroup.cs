namespace Metamatic.UI {
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    [ExecuteAlways]
    public class VerticalTiledLayoutGroup : MonoBehaviour
    {
        public bool fitContent = true;
        public bool fitContentWidth = false;
        public float paddingTop = 0f;
        public float paddingBottom = 0f;
        public float spacing = 0;
        RectTransform rect;
        DrivenRectTransformTracker dt;
        private event System.Action OnBeforeDisable;
        void Awake() {
            rect = (RectTransform)transform;
            dt = new DrivenRectTransformTracker();
        }
        void OnEnable() {
            dt.Clear();
            dt.Add(this, rect, DrivenTransformProperties.SizeDeltaY);

            foreach(RectTransform child in transform) {
                var dt = new DrivenRectTransformTracker();
                dt.Clear();
                dt.Add(this, child, DrivenTransformProperties.AnchoredPositionY);

                ClearTrackerOnDestroy(dt);
            }
        }

        void ClearTrackerOnDestroy(DrivenRectTransformTracker dt) {
            OnBeforeDisable += () => {
                dt.Clear();
            };
        }

        void OnDisable() {
            dt.Clear();
            OnBeforeDisable?.Invoke();
        }
        void Start() {
            foreach(RectTransform child in transform) {
                child.anchorMax = new Vector2(
                    child.anchorMax.x,
                    1
                );
                child.anchorMin = new Vector2(
                    child.anchorMin.x,
                    1
                );
                child.pivot = new Vector2(
                    child.pivot.x,
                    1
                );
            }
        }

        void Update()
        {
            float top = paddingTop;
            int index = 0;
            foreach(RectTransform child in transform) {
                child.anchoredPosition = new Vector3(
                    child.anchoredPosition.x,
                    top
                );
                top -= child.sizeDelta.y;

                 if (index < transform.childCount - 1) {
                    top -= spacing;
                }
                index++;
            }
            top -= paddingBottom;

            var width = 0f;

            if (fitContentWidth) {
                foreach(RectTransform child in transform) {
                    width = Mathf.Max(width, child.sizeDelta.x);
                }
            } else {
                width = rect.sizeDelta.x;
            }

            if (fitContent) {
                rect.sizeDelta = new Vector2(
                    width,
                    -top
                );
            }
        }

#if UNITY_EDITOR
        void OnValidate() {
            dt.Clear();
            if (fitContent && rect != null) {
                dt.Add(this, rect, DrivenTransformProperties.SizeDeltaY);
            }
        }
#endif
    }
}