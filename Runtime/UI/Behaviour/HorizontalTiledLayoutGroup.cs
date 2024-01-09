namespace Metamatic.UI {
    using UnityEngine;

    [ExecuteAlways]
    public class HorizontalTiledLayoutGroup : MonoBehaviour
    {
        public float paddingLeft = 0f;
        public float paddingRight = 0f;
        public float spacing = 0;
        public bool fitContent = true;
        public bool smooth = false;
        public float smoothSpeed = 6f;
        public int minWidth = 0;
        public int maxWidth = 99999;
        RectTransform rect;
        DrivenRectTransformTracker dt = new DrivenRectTransformTracker();
        DrivenRectTransformTracker dtChild = new DrivenRectTransformTracker();
        private event System.Action OnBeforeDisable;
        void Awake() {
            rect = (RectTransform)transform;
        }
        void OnEnable() {
            dt.Clear();
            if (fitContent && rect != null) {
                dt.Add(this, rect, DrivenTransformProperties.SizeDeltaX);
            }

            ProcessChild();
        }

        private void ProcessChild()
        {
            dtChild.Clear();
            foreach(RectTransform child in transform) {
                
                dtChild.Add(this, child, DrivenTransformProperties.AnchoredPositionX);
            }
        }
        
        private void OnTransformChildrenChanged()
        {
            ProcessChild();
        }

        void OnDisable() {
            dt.Clear();
            dtChild.Clear();
            OnBeforeDisable?.Invoke();
        }
        void Start() {
            foreach(RectTransform child in transform) {
                child.anchorMax = new Vector2(
                    0,
                    child.anchorMax.y
                );
                child.anchorMin = new Vector2(
                    0,
                    child.anchorMin.y
                );
                child.pivot = new Vector2(
                    0,
                    child.pivot.y
                );
            }
        }

        void LateUpdate()
        {
            float left = paddingLeft;
            int index = 0;
            foreach(RectTransform child in transform) {
                child.anchoredPosition = new Vector3(
                    left,
                    child.anchoredPosition.y
                );
                left += child.sizeDelta.x;

                if (index < transform.childCount - 1) {
                    left += spacing;
                }
                index++;
            }
            if (fitContent) {
                var finalWidth = Mathf.Min(Mathf.Max(minWidth + paddingRight, left + paddingRight), maxWidth);
                if (smooth) {
                    rect.sizeDelta = Vector2.Lerp(rect.sizeDelta, new Vector2(
                        finalWidth,
                        rect.sizeDelta.y
                    ), Time.deltaTime * smoothSpeed);
                } else {
                    rect.sizeDelta = new Vector2(
                        finalWidth,
                        rect.sizeDelta.y
                    );
                }
            }
        }

#if UNITY_EDITOR
        void OnValidate() {
            dt.Clear();
            if (fitContent && rect != null) {
                dt.Add(this, rect, DrivenTransformProperties.SizeDeltaX);
            }
        }
#endif
    }
}