using System;
using UnityEngine;
using UnityEngine.EventSystems;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
#endif
using Metamatic.DevTools;

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

        private Vector2 m_StartPosition = Vector2.zero;
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
                        foreach(var parent in disabledSwipeParents) {
                            parent.disabled = true;
                        }
                    } else {
                        foreach(var parent in disabledSwipeParents) {
                            parent.disabled = false;
                        }
                    }
                }
            }
        }

        private RectTransform rectTransform;
        private RectTransform movableRectTransform;
        private bool pointerEntered = false;
        #endregion

        private void Awake() {
            #if ENABLE_INPUT_SYSTEM
            EnhancedTouchSupport.Enable();
            #endif
        }

        private void OnEnable() {
            rectTransform = GetComponent<RectTransform>();
        }

        private void OnDisable() {
            foreach(var parent in disabledSwipeParents) {
                parent.disabled = false;
            }
        }

        private void Update() {
            #region Global input
            if (pointerIsEnabled) {
                #region Standalone Input
                
                if (
                    (useGlobalInput || pointerEntered)
                    #if ENABLE_INPUT_SYSTEM
                    && Mouse.current.leftButton.wasPressedThisFrame
                    #else
                    && Input.GetMouseButtonDown(0)
                    #endif
                ) {
                   
                    isPressed = true;
                    #if ENABLE_INPUT_SYSTEM
                    userStartPosition = (Vector2)Mouse.current.position.ReadValue();
                    #else
                    userStartPosition = (Vector2)Input.mousePosition;
                    #endif
                }
                #if ENABLE_INPUT_SYSTEM
                if (Mouse.current.leftButton.wasReleasedThisFrame) {
                #else
                if (Input.GetMouseButtonUp(0)) {
                #endif
                    isPressed = false;
                }
                #endregion

                #region Mobile Input
                #if ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count > 0) {
                #else
                if (Input.touchCount > 0)
                {
#endif
                    #if ENABLE_INPUT_SYSTEM
                    if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0].phase == UnityEngine.InputSystem.TouchPhase.Began) {
                         isPressed = true;
                        userStartPosition = Input.touches[0].position;
                    } else if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0].phase == UnityEngine.InputSystem.TouchPhase.Ended || UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0].phase == UnityEngine.InputSystem.TouchPhase.Canceled) {
                        isPressed = false;
                    }
                    #else
                    switch (Input.touches[0].phase)
                    {
                        case TouchPhase.Began:
                            isPressed = true;
                            userStartPosition = Input.touches[0].position;
                            break;
                        case UnityEngine.TouchPhase.Ended:
                        case UnityEngine.TouchPhase.Canceled:
                            isPressed = false;
                            break;
                        case TouchPhase.Moved:
                            break;
                        case TouchPhase.Stationary:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    #endif
                }
                #endregion
            }
            #endregion

            if (isPressed) {
                #region Global input
                if (pointerIsEnabled) {
                    #if ENABLE_INPUT_SYSTEM
                    if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count > 0) {
                    #else
                    if (Input.touchCount > 0) {
                    #endif
                        distance = distance + new Vector2(
                            #if ENABLE_INPUT_SYSTEM
                            UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0].screenPosition.x - userStartPosition.x,
                            UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0].screenPosition.y - userStartPosition.y
                            #else
                            Input.touches[0].position.x - userStartPosition.x,
                            Input.touches[0].position.y - userStartPosition.y
                            #endif
                        );
                        magnitude = new Vector2(
                            #if ENABLE_INPUT_SYSTEM
                            prevPointerPosition.x - UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0].screenPosition.x,
                            prevPointerPosition.y - UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0].screenPosition.y
                            #else
                            prevPointerPosition.x - Input.touches[0].position.x,
                            prevPointerPosition.y - Input.touches[0].position.y
                            #endif
                        ) / Time.deltaTime;
                        #if ENABLE_INPUT_SYSTEM
                        prevPointerPosition = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0].screenPosition;
                        #else
                        prevPointerPosition = (Vector2)Input.touches[0].position;
                        #endif
                    } else {
                        #if ENABLE_INPUT_SYSTEM
                        var mousePositon = (Vector2)Mouse.current.position.ReadValue();
                        #else
                        var mousePositon = (Vector2) Input.mousePosition;
                        #endif
                        distance = new Vector2(
                            #if ENABLE_INPUT_SYSTEM
                            mousePositon.x - userStartPosition.x,
                            mousePositon.y - userStartPosition.y
                            #else
                            Input.mousePosition.x - userStartPosition.x,
                            Input.mousePosition.y - userStartPosition.y
                            #endif
                        );
                        magnitude = new Vector2(
                            #if ENABLE_INPUT_SYSTEM
                                mousePositon.x - prevPointerPosition.x,
                                mousePositon.y - prevPointerPosition.y
                            #else
                                Input.mousePosition.x - prevPointerPosition.x,
                                Input.mousePosition.y - prevPointerPosition.y
                            #endif
                            
                        ) / Time.deltaTime;
                        #if ENABLE_INPUT_SYSTEM
                        prevPointerPosition = mousePositon;
                        #else
                        prevPointerPosition = (Vector2)Input.touches[0].position;
                        #endif
                    }
                }
                #endregion

                if (!stopPropagation)
                {
                    switch (orientation)
                    {
                        case Orientation.HORIZONTAL:
                        {
                            if (Mathf.Abs(magnitude.x) > stopPropagationMagnitude) {
                                stopPropagation = true;
                            }

                            break;
                        }
                        case Orientation.VERTICAL:
                        {
                            if (Mathf.Abs(magnitude.y) > stopPropagationMagnitude) {
                                stopPropagation = true;
                            }

                            break;
                        }
                        case Orientation.BOTH:
                        {
                            if (Mathf.Abs(magnitude.x) > stopPropagationMagnitude || Mathf.Abs(magnitude.y) > stopPropagationMagnitude) {
                                stopPropagation = true;
                            }

                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            } else {
                magnitude = Vector2.zero;
                stopPropagation = false;
            }

            /* Calculate swipe ratio */
            var rect = rectTransform.rect;
            magnitudeRatio = new Vector2(
                magnitude.x / rect.width,
                magnitude.y / rect.height
            );
            ratio = new Vector2(
                distance.x / rect.width,
                distance.y / rect.height
            );
            UILog.Add("Ratio", ratio);
        }

        public void Release() {
            isPressed = false;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Called when the script is loaded or a value is changed in the
        /// inspector (Called in the editor only).
        /// </summary>
        private void OnValidate()
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