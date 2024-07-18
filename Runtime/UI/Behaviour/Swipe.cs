using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Metamatic.DevTools;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
#endif

namespace Metamatic.UI {
    /// <summary>
    /// Allows to swipe inner child with pointer and got event OnSwipeLeft, OnSwipeRight (using RectMask2D is recommended for game object.).
    /// </summary>
    /// <remarks>This class is not stable to use with built-in input and require development to support it. I works fine only with InputSystem.</remarks>
    public class Swipe: MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerMoveHandler {
        public class FingerInfo
        {
            public int index;
            public Vector2 startPosition;
            public Vector2 prevPosition;
            public Vector2 distance;
            public Vector2 magnitude;
        }
        public enum Orientation {
            HORIZONTAL,
            VERTICAL,
            BOTH
        }
        #region Fields
        /// <summary>
        /// Swipe event user use pointer outside the container
        /// </summary>
        public bool useGlobalInput;
        public Orientation orientation = Orientation.BOTH;
        /// <summary>
        /// Prevent handling pointer of the parent Swipe components when this one have non-zero magnitude
        /// </summary>
        public float stopPropagationMagnitude;
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
                return m_PointerPressedCount > 0;
            }
        }
        public Dictionary<int, FingerInfo> fingerInfos = new Dictionary<int, FingerInfo>();
        #if DEBUG
        public bool debug;
        #endif
        #endregion
        
        private Canvas m_Canvas;
        public Canvas canvas {
            get {
                if (m_Canvas == null) {
                    m_Canvas = GetComponentInParent<Canvas>();
                }
                return m_Canvas;
            }
        }

        
        #region events

        #endregion

        #region internal
        public bool m_Disabled;
        public bool disabled {
            get {
                return m_Disabled;
            }
            set {
                var changed = value != m_Disabled;
                m_Disabled = value;
                if (changed) {
                    if (value) {
                        allowInteractPointer = false;
                    } else {
                        allowInteractPointer = true;
                    }
                }
            }
        }

        private Vector2 m_StartPosition = Vector2.zero;
        public Vector2 mouseStartPosition {
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
        private bool allowInteractPointer = true;
        private bool m_StopPropagation;
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
        private int m_PointerEnteredCount;
        private int m_PointerPressedCount;
        private bool m_IsTouchPointer;
        private bool m_TouchPointerActiveOnElement = false;
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

        private void Start()
        {
            #if DEBUG
            if (debug)
            {
                UILog.Add("Swipe", "magnitudeRatio", () => magnitudeRatio);
                UILog.Add("Swipe", "ratio", () => ratio);
                UILog.Add("Swipe", "magnitude", () => magnitude);
                UILog.Add("Swipe", "distance", () => distance);
                UILog.Add("Swipe", "allowInteractPointer", () => allowInteractPointer);
                UILog.Add("Swipe", "Pointers count", () => m_PointerEnteredCount);
                UILog.Add("Swipe", "Touchscreen support", () => IsTouchScreenActive());
            }
            #endif
        }

        private void Update() {
            
            if (!allowInteractPointer || m_PointerPressedCount == 0)
            {
                magnitude = Vector2.zero;
                distance = Vector2.zero;
                stopPropagation = false;
                magnitudeRatio = Vector2.zero;
                ratio = Vector2.zero;
                mouseStartPosition = Vector2.zero;
                prevPointerPosition = Vector2.zero;
                return;
            }
            

            if (IsTouchScreenActive())
            {
                UpdateMobileInput();
            }
            else
            {
                UpdateStandAloneInput();
            }

            UpdateStopPropagation();

            /* Calculate swipe ratio */
            float scaleFactor = canvas.scaleFactor;
            var rect = rectTransform.rect;
            float width = rect.width * scaleFactor;
            float height = rect.height * scaleFactor;
            magnitudeRatio = new Vector2(
                magnitude.x / width,
                magnitude.y / height
            );
            ratio = new Vector2(
                distance.x / width,
                distance.y / height
            );
        }

        private bool IsTouchScreenActive()
        {
#if ENABLE_INPUT_SYSTEM
            return Touchscreen.current != null && m_IsTouchPointer;
#else
            if (!Input.touchSupported)
            {
                return false;
            }
#endif
            return true;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateMobileInput()
        {
            /* Ignore method if touch is not supported */
            #if ENABLE_INPUT_SYSTEM
            Vector2 leadingMagnitude = Vector2.zero;
            Vector2 leadingDistance = Vector2.zero;
            foreach (var touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
            {
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    fingerInfos[touch.finger.index] = new FingerInfo
                    {
                        index = touch.finger.index,
                        startPosition = touch.screenPosition,
                        prevPosition = touch.screenPosition,
                        distance = Vector2.zero,
                        magnitude = Vector2.zero
                    };
                } else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended || touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    fingerInfos.Remove(touch.finger.index);
                }
                else
                {
                    var fingerInfo = fingerInfos[touch.finger.index];
                    fingerInfo.distance = fingerInfo.distance + new Vector2(
                        touch.screenPosition.x - fingerInfo.startPosition.x,
                        touch.screenPosition.y - fingerInfo.startPosition.y
                    );
                    fingerInfo.magnitude = new Vector2(
                        touch.screenPosition.x - fingerInfo.prevPosition.x,
                        touch.screenPosition.y - fingerInfo.prevPosition.y
                    ) / Time.deltaTime;
                    fingerInfo.prevPosition = touch.screenPosition;
                    if (leadingMagnitude.magnitude < fingerInfo.magnitude.magnitude)
                    {
                        leadingMagnitude = fingerInfo.magnitude;
                    }
                    if (leadingDistance.magnitude < fingerInfo.distance.magnitude)
                    {
                        leadingDistance = fingerInfo.distance;
                    }
                }
            }
            #else
            foreach(var nativeTouch in Input.touches) {
                if (nativeTouch.phase == TouchPhase.Began) {
                    fingerInfos[nativeTouch.fingerId] = new FingerInfo {
                        index = nativeTouch.fingerId,
                        startPosition = nativeTouch.position,
                        prevPosition = nativeTouch.position,
                        distance = Vector2.zero,
                        magnitude = Vector2.zero
                    };
                } else if (nativeTouch.phase == TouchPhase.Ended || nativeTouch.phase == TouchPhase.Canceled) {
                    fingerInfos.Remove(nativeTouch.fingerId);
                } else {
                    var fingerInfo = fingerInfos[nativeTouch.fingerId];
                    fingerInfo.distance = fingerInfo.distance + new Vector2(
                        nativeTouch.position.x - fingerInfo.startPosition.x,
                        nativeTouch.position.y - fingerInfo.startPosition.y
                    );
                    fingerInfo.magnitude = new Vector2(
                        nativeTouch.position.x - fingerInfo.prevPosition.x,
                        nativeTouch.position.y - fingerInfo.prevPosition.y
                    ) / Time.deltaTime;
                    fingerInfo.prevPosition = nativeTouch.position;

                    if (leadingMagnitude.magnitude < fingerInfo.magnitude.magnitude)
                    {
                        leadingMagnitude = fingerInfo.magnitude;
                    }
                    if (leadingDistance.magnitude < fingerInfo.distance.magnitude)
                    {
                        leadingDistance = fingerInfo.distance;
                    }
                }
            }
            #endif
            distance = leadingDistance;
            magnitude = leadingMagnitude;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateStandAloneInput()
        {
            if (
                (useGlobalInput || m_PointerEnteredCount > 0)
#if ENABLE_INPUT_SYSTEM
                && Mouse.current.leftButton.wasPressedThisFrame
#else
                && Input.GetMouseButtonDown(0)
#endif
            )
            {
#if ENABLE_INPUT_SYSTEM
                mouseStartPosition = (Vector2)Mouse.current.position.ReadValue();
#else
                mouseStartPosition = (Vector2)Input.mousePosition;
#endif
                prevPointerPosition = mouseStartPosition;
            }
            
            if (!isPressed)
            {
                return;
            }
            
#if ENABLE_INPUT_SYSTEM
            var mousePositon = (Vector2)Mouse.current.position.ReadValue();
#else
                    var mousePositon = (Vector2) Input.mousePosition;
#endif
            distance = new Vector2(
#if ENABLE_INPUT_SYSTEM
                mousePositon.x - mouseStartPosition.x,
                mousePositon.y - mouseStartPosition.y
#else
                        Input.mousePosition.x - mouseStartPosition.x,
                        Input.mousePosition.y - mouseStartPosition.y
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
                    prevPointerPosition = (Vector2)Input.mousePosition;
#endif
        }

        private void UpdateStopPropagation()
        {
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
        }

#if UNITY_EDITOR
        /// <summary>
        /// Called when the script is loaded or a value is changed in the
        /// inspector (Called in the editor only).
        /// </summary>
        private void OnValidate()
        {
            
        }

        private void OnDrawGizmos()
        {
            #if UNITY_EDITOR
            var camera = Camera.main;
            if (!Application.isPlaying || camera == null) {
                return;
            }

            Vector3 worldMouseStartPosition = camera.ScreenToWorldPoint(new Vector3(mouseStartPosition.x, mouseStartPosition.y, camera.nearClipPlane));
            Vector3 worldPrevPointerPosition = camera.ScreenToWorldPoint(new Vector3(prevPointerPosition.x, prevPointerPosition.y, camera.nearClipPlane));
            var circleRadius = HandleUtility.GetHandleSize(worldMouseStartPosition) * 0.1f;
            /* Рисуем сферы и линии с использованием мировых координат */
            Handles.color = new Color(0.13f, 0.55f, 0.82f);
            Handles.DrawWireArc(worldMouseStartPosition, Vector3.forward, Vector3.up, 360, circleRadius); // Измените размер сферы при необходимости

            Handles.color = new Color(0.82f, 0.53f, 0.16f);
            Handles.DrawWireArc(worldPrevPointerPosition, Vector3.forward, Vector3.up, 360, circleRadius); // Измените размер сферы при необходимости

            Handles.color = new Color(0.05f, 0.59f, 0.91f);
            Handles.DrawDottedLine(worldMouseStartPosition, worldPrevPointerPosition, 2);
            #endif
        }
#endif

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) {
            m_PointerEnteredCount++;
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData) {
            m_PointerEnteredCount--;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            m_IsTouchPointer = eventData.pointerId != -1;
            m_PointerPressedCount++;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            UILog.Add("Pointer", "Down", "False");
            m_PointerPressedCount--;
            if (m_PointerPressedCount == 0)
            {
                
            }
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            
        }
        
    }
}