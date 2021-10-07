using System;
using UnityEngine;
using UnityEngine.EventSystems;
#if ODIN_INSPECTOR_3
using Sirenix.OdinInspector;
#endif

namespace Metamatic.UI {
    /// <summary>
    /// Allows to swipe inner child with pointer and got event OnSwipeLeft, OnSwipeRight (using RectMask2D is recommended for game object.).
    /// </summary>
    public class Swipe: MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
        public enum Orientation {
            HORIZONTAL,
            VERTICAL,
            BOTH
        }
        #region Fields
        /// <summary>
        /// Swipe event user use pointer outside the container
        /// </summary>
        public bool useGlobalInput = false;
        public Orientation orientation = Orientation.BOTH;
        /// <summary>
        /// Prevent handling pointer of the parent Swipe components when this one have non-zero magnitude
        /// </summary>
        public float stopPropagationMagnitude = 0f;
        #endregion

        #region program api
        #if ODIN_INSPECTOR_3
        [ShowInInspector, ReadOnly]
        #endif
        public Vector2 ratio {
            get; private set;
        } = Vector2.zero;
        #if ODIN_INSPECTOR_3
        [ShowInInspector, ReadOnly]
        #endif
        public Vector2 magnitude {
            get; private set;
        } = Vector2.zero;
        [ShowInInspector, ReadOnly]
        public Vector2 magnitudeRatio {
            get; private set;
        }
        /// <summary>
        /// The distance that pointer moved from the start position
        /// </summary>
        /// <value></value>
        #if ODIN_INSPECTOR_3
        [ShowInInspector, ReadOnly]
        #endif
        public Vector2 distance {
            get; private set;
        } = Vector2.zero;
        private bool m_IsPressed = false;
        #if ODIN_INSPECTOR_3
        [ShowInInspector, ReadOnly]
        #endif
        public bool isPressed {
            get {
                return m_IsPressed;
            }
            private set {
                var changed = value != m_IsPressed;
                m_IsPressed = value;
            }
        }
        #endregion

        
        #region events
        public event Action OnSwipeLeft;
        public event Action OnSwipeRight;
        public event Action OnSwipeMax;
        #endregion

        #region internal
        public bool m_Disabled = false;
        public bool disabled {
            get {
                return m_Disabled;
            }
            set {
                var changed = value != m_Disabled;
                m_Disabled = value;
                if (changed) {
                    if (value) {
                        isPressed = false;
                        pointerIsEnabled = false;
                    } else {
                        pointerIsEnabled = true;
                    }
                }
            }
        }
        Vector2 m_StartPosition = Vector2.zero;
        public Vector2 userStartPosition {
            get {
                return m_StartPosition;
            }
            set {
                m_StartPosition = value;
                prevPointerPosition = value;
            }
        }
        private Vector2 prevPointerPosition = Vector2.zero;
        private Vector2 prevDistance = Vector2.zero;
        /// <summary>
        /// User can use pointer
        /// </summary>
        private bool pointerIsEnabled = true;
        private bool m_StopPropagation = false;
        private Swipe[] disabledSwipeParents = new Swipe[0];

        #if ODIN_INSPECTOR_3
        [ShowInInspector, ReadOnly]
        #endif
        private bool stopPropagation {
            get {
                return m_StopPropagation;
            }
            set {
                var changed = value != m_StopPropagation;
                m_StopPropagation = value;
                if (changed) {
                    if (value && transform.parent != null) {
                        disabledSwipeParents = transform.parent.GetComponentsInParent<Swipe>();
                        foreach(Swipe parent in disabledSwipeParents) {
                            parent.disabled = true;
                        }
                    } else {
                        foreach(Swipe parent in disabledSwipeParents) {
                            parent.disabled = false;
                        }
                    }
                }
            }
        }
        RectTransform rectTransform;
        RectTransform movableRectTransform;
        bool pointerEntered = false;
        #endregion

        void OnEnable() {
            rectTransform = GetComponent<RectTransform>();
        }

        void OnDisable() {
            foreach(Swipe parent in disabledSwipeParents) {
                parent.disabled = false;
            }
        }

        void Update() {
            #region Global input
            if (pointerIsEnabled) {
                #region Standalone Input
                if ((useGlobalInput || pointerEntered) && Input.GetMouseButtonDown(0)) {
                    /// <summary>
                    /// Determins that user press pointer (mouse or touchsreen)
                    /// </summary>
                    isPressed = true;
                    /// <summary>
                    /// Memorize start pointer position
                    /// </summary>
                    /// <returns></returns>
                    userStartPosition = (Vector2)Input.mousePosition;
                }
                if (Input.GetMouseButtonUp(0)) {
                    isPressed = false;
                }
                #endregion

                #region Mobile Input
                if (Input.touchCount > 0) {
                    if (Input.touches[0].phase == TouchPhase.Began) {
                        isPressed = true;
                        userStartPosition = Input.touches[0].position;
                    } else if (Input.touches[0].phase == TouchPhase.Ended || Input.touches[0].phase == TouchPhase.Canceled) {
                        isPressed = false;
                    }
                }
                #endregion
            }
            #endregion

            if (isPressed) {
                #region Global input
                if (pointerIsEnabled) {
                    if (Input.touchCount > 0) {
                        distance = distance + new Vector2(
                            Input.touches[0].position.x - userStartPosition.x,
                            Input.touches[0].position.y - userStartPosition.y
                        );
                        magnitude = new Vector2(
                            prevPointerPosition.x - Input.touches[0].position.x,
                            prevPointerPosition.y - Input.touches[0].position.y
                        ) / Time.deltaTime;;
                        prevPointerPosition = (Vector2)Input.touches[0].position;
                    } else {
                        distance = distance + new Vector2(
                            Input.mousePosition.x - userStartPosition.x,
                            Input.mousePosition.y - userStartPosition.y
                        );
                        magnitude = new Vector2(
                            Input.mousePosition.x - prevPointerPosition.x,
                            Input.mousePosition.y - prevPointerPosition.y
                        ) / Time.deltaTime;
                        prevPointerPosition = (Vector2)Input.mousePosition;
                    }
                }
                #endregion

                if (!stopPropagation) {
                    if (orientation == Orientation.HORIZONTAL) {
                        if (Mathf.Abs(magnitude.x) > stopPropagationMagnitude) {
                            stopPropagation = true;
                        }
                    } else if (orientation == Orientation.VERTICAL) {
                        if (Mathf.Abs(magnitude.y) > stopPropagationMagnitude) {
                            stopPropagation = true;
                        }
                    } else {
                        if (Mathf.Abs(magnitude.x) > stopPropagationMagnitude || Mathf.Abs(magnitude.y) > stopPropagationMagnitude) {
                            stopPropagation = true;
                        }
                    }
                }
            } else {
                magnitude = Vector2.zero;
                stopPropagation = false;
            }

            /* Calculate swipe ratio */
            magnitudeRatio = new Vector2(
                magnitude.x / rectTransform.rect.width,
                magnitude.y / rectTransform.rect.height
            );
            ratio = new Vector2(
                distance.x / rectTransform.rect.width,
                distance.y / rectTransform.rect.height
            );
        }

        public void Release() {
            isPressed = false;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Called when the script is loaded or a value is changed in the
        /// inspector (Called in the editor only).
        /// </summary>
        void OnValidate()
        {
            
        }
#endif

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) {
            pointerEntered = true;
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData) {
            pointerEntered = false;
        }
    }
}