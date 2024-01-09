using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Metamatic.UI {
    using UnityEngine;

    [ExecuteAlways]
    public class VerticalTiledLayoutGroup : UIBehaviour
    {
        public bool fitContent = true;
        public bool fitContentWidth = false;
        public float paddingTop = 0f;
        public float paddingBottom = 0f;
        public float paddingLeft = 0f;
        public float paddingRight = 0f;
        public float spacing = 0;
        /// <summary>
        /// Update size each frame
        /// </summary>
        public bool updateEachFrame = false;
        private float m_UpdateInterval = 0.1f; // 10 раз в секунду (1 секунда / 10)
        private float m_NextUpdateTime = 0.0f;
        private RectTransform m_Rect;
        RectTransform rect => m_Rect ??= GetComponent<RectTransform>();
        DrivenRectTransformTracker dt;
        private event System.Action OnBeforeDisable;
        void Awake() {
            dt = new DrivenRectTransformTracker();
        }
        void OnEnable() {
            dt.Clear();
            if (fitContent) {
                dt.Add(this, rect, DrivenTransformProperties.SizeDeltaY);
            }
            if (fitContentWidth) {
                dt.Add(this, rect, DrivenTransformProperties.SizeDeltaX);
            }

            foreach(RectTransform child in transform) {
                if (child.gameObject.activeSelf == false) {
                    continue;
                }
                var dt = new DrivenRectTransformTracker();
                dt.Clear();
                dt.Add(this, child, DrivenTransformProperties.AnchoredPositionY);

                ClearTrackerOnDestroy(dt);
            }
            
            SetDirty();
        }

        void ClearTrackerOnDestroy(DrivenRectTransformTracker dt) {
            OnBeforeDisable += () => {
                dt.Clear();
            };
        }

        void OnDisable()
        {
            SetDirty();
            dt.Clear();
            OnBeforeDisable?.Invoke();
        }
        void Start() {
            foreach(RectTransform child in transform) {
                if (child.gameObject.activeSelf == false) {
                    continue;
                }
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
            if (!updateEachFrame)
            {
                SetDirty();
            } else if (Time.time >= m_NextUpdateTime)
            {
                m_NextUpdateTime = Time.time + m_UpdateInterval;
                SetDirty();
            }
        }

        void SetDirty()
        {
            float top = -paddingTop;
            int index = 0;
            foreach(RectTransform child in transform) {
                if (child.gameObject.activeSelf == false) {
                    continue;
                }

                if (fitContentWidth || fitContent)
                {
                    child.anchorMin = new Vector2(fitContentWidth ? 0.5f : child.anchorMin.x, fitContent ? 1 : child.anchorMin.y);
                    child.anchorMax = new Vector2(fitContentWidth ? 0.5f : child.anchorMax.x, fitContent ? 1 : child.anchorMax.y);
                    child.pivot = new Vector2(
                        fitContentWidth ? 0.5f : child.pivot.x,
                        fitContent ? 1 : child.pivot.y
                    );
                }
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
                    if (child.gameObject.activeSelf == false) {
                        continue;
                    }
                    width = Mathf.Max(width, child.sizeDelta.x) + paddingLeft + paddingRight;
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
            if (fitContent) {
                dt.Add(this, rect, DrivenTransformProperties.SizeDeltaY);
            }
            if (fitContentWidth) {
                dt.Add(this, rect, DrivenTransformProperties.SizeDeltaX);
            }
            if (!IsActive())
                return;
            SetDirtyNextFrame();
        }
        
        private void SetDirtyNextFrame() {
            UnityEditor.EditorApplication.delayCall += () => {
                SetDirty();
            };
        }
#endif
    }
}