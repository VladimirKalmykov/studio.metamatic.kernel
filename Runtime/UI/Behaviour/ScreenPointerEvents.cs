namespace Metamatic.UI {
    using UnityEngine;
    using Sirenix.OdinInspector;
    using Input = UnityEngine.InputSystem.EnhancedTouch;
    using UnityEngine.InputSystem;
    using UnityEngine.UI;
    using UnityEngine.EventSystems;

    public class ScreenPointerEvents : MonoBehaviour, ISerializationCallbackReceiver
    {
        public Graphic area;
        /* Fields */
        [BoxGroup("Movement")]
        public Vector2 movementmMultiplier = Vector2.one;
        [BoxGroup("Movement")]
        public bool movementLimit;
        [BoxGroup("Movement"), ShowIf("movementLimit")]
        public Vector2 movementNegativeLimit = new Vector2(-999999f, -999999f);
        [BoxGroup("Movement"), ShowIf("movementLimit")]
        public Vector2 movementPositiveLimit = new Vector2(999999f, 999999f);
        [BoxGroup("Movement")]
        public float movementSpring;
        [BoxGroup("Movement")]
        [Tooltip("Reset movement when user releases button")]
        public bool resetOnRelease;
        [BoxGroup("Scale")]
        public float maxScale = 4f;
        [BoxGroup("Scale")]
        public float minScale = 0.1f;
        [BoxGroup("Scale")]
        public float mouseScrollSize = 1200f;
        [BoxGroup("Scale")]
        [Tooltip("Set value greater then zero to smooth reset scale")]
        public float scaleSpring = 1f;
        [BoxGroup("Scale")]
        public bool invertScale;
        
        /* Public properties */
        [ShowInInspector, ReadOnly, HideInEditorMode, System.NonSerialized]
        public Vector2 movement = Vector2.zero;
        [ShowInInspector, ReadOnly, HideInEditorMode, System.NonSerialized]
        public float scale = 1f;
        public Vector2 movementYX {
            get {
                return new Vector2(movement.y, movement.x);
            }
        }

        /* Private fields */
        /// <summary>
        /// Accumulate movement delta while mouse button or fingers are pressed
        /// </summary>
        private Vector2 m_TotalDelta = Vector2.zero;
        private PointerEvents areaPointerEvents;
        private bool m_PointerIsPressed;

        /* */
        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize() {
            scaleSpring = Mathf.Max(0f, scaleSpring);
            minScale = Mathf.Max(0f, minScale);
            movementSpring = Mathf.Max(0f, movementSpring);
        }

        [Button]
        public void Reset() {
            m_TotalDelta = Vector2.zero;
            movement = Vector2.zero;
        }

        void OnEnable() {
            Input.EnhancedTouchSupport.Enable();
            Input.Touch.onFingerUp += HandleFingerUp;
            if (area == null) {
                Debug.LogWarning("ScreenPointerEvents requires area");
                enabled = false;
                return;
            }
            areaPointerEvents = area.GetComponent<PointerEvents>() ?? area.gameObject.AddComponent<PointerEvents>();
            areaPointerEvents.OnDown.AddListener(HandlePointerDown);
            areaPointerEvents.OnUp.AddListener(HandlePointerUp);
        }

        void OnDisable() {
            if (areaPointerEvents != null) {
                areaPointerEvents.OnDown.RemoveListener(HandlePointerDown);
                areaPointerEvents.OnUp.RemoveListener(HandlePointerUp);
            }
            Input.Touch.onFingerUp -= HandleFingerUp;
        }
        
        void HandleFingerUp(Input.Finger finger) {
            if ((movementSpring > 0f || resetOnRelease) && Input.Touch.activeTouches.Count == 0) {
                m_TotalDelta = Vector3.zero;
            }
        }
        
        void HandlePointerDown(PointerEventData eventData) {
            m_PointerIsPressed = true;
        }

        void HandlePointerUp(PointerEventData eventData) {
            m_PointerIsPressed = false;
        }
        
        void Update() {
            if ((movementSpring > 0f || resetOnRelease) && Mouse.current.leftButton.wasReleasedThisFrame) {
                m_TotalDelta = Vector3.zero;
            }

            /* Calculate scale factor */
            var scrollY = Mouse.current.scroll.y.ReadValue() * (invertScale ? -1f : 1f);

            if (scrollY != 0f) {
                float nextScale =  1 + scrollY / mouseScrollSize;
                scale = Mathf.Max(Mathf.Min(maxScale, scale * nextScale), minScale);
            }
            scale = Mathf.Lerp(scale, 1f, Time.deltaTime * scaleSpring);

            var x = movement.x;
            var y = movement.y;
            
            /* Move by fingers */
            if (m_PointerIsPressed && Input.Touch.activeTouches.Count > 0) {
                switch (Input.Touch.activeTouches[0].phase) {
                    case UnityEngine.InputSystem.TouchPhase.Moved:
                        var delta = Input.Touch.activeTouches[0].delta;
                        m_TotalDelta += new Vector2(
                            delta.x * movementmMultiplier.x,
                            delta.y * movementmMultiplier.y
                        );
                        // move = new Vector3(
                        //     Mathf.Min(Mathf.Max(limitMin.x, move.x + moveDelta.x), limitMax.x),
                        //     Mathf.Min(Mathf.Max(limitMin.y, move.y + moveDelta.y), limitMax.y),
                        //     Mathf.Min(Mathf.Max(limitMin.z, move.z + moveDelta.z), limitMax.z)
                        // );

                        x = m_TotalDelta.x / Screen.width;
                        y = m_TotalDelta.y / Screen.height;
                        if (movementLimit) {
                            x = Mathf.Max(movementNegativeLimit.x, Mathf.Min(movementPositiveLimit.x, x));
                            y = Mathf.Max(movementNegativeLimit.y, Mathf.Min(movementPositiveLimit.y, y));
                        }
                    break;
                }
            }
            /* Move by mouse */
            else if (m_PointerIsPressed && Mouse.current.leftButton.isPressed) {
                    m_TotalDelta += new Vector2(
                        Mouse.current.delta.x.ReadValue() * movementmMultiplier.x,
                        Mouse.current.delta.y.ReadValue() * movementmMultiplier.y
                    );
                    x = m_TotalDelta.x / Screen.width;
                    y = m_TotalDelta.y / Screen.height;
                    if (movementLimit) {
                        x = Mathf.Max(movementNegativeLimit.x, Mathf.Min(movementPositiveLimit.x, x));
                        y = Mathf.Max(movementNegativeLimit.y, Mathf.Min(movementPositiveLimit.y, y));
                    }
                    // var elasticityX = Mathf.Min(1f, nextX >= 0f ? (limitMax.x != 0 ? Mathf.Pow(nextX / limitMax.x, 2f) : 1f) : (limitMin.x != 0 ? Mathf.Pow(nextX / limitMin.x, 2f) : 1f));
                    // var elasticityY = Mathf.Min(1f, nextY >= 0f ? (limitMax.y != 0 ? Mathf.Pow(nextY / limitMax.y, 2f) : 1f) : (limitMin.y != 0 ? Mathf.Pow(nextX / limitMin.y, 2f) : 1f));
                    // var elasticityZ = Mathf.Min(1f, nextZ >= 0f ? (limitMax.z != 0 ? Mathf.Pow(nextZ / limitMax.z, 2f) : 1f) : (limitMin.z != 0 ? Mathf.Pow(nextZ / limitMin.z, 2f) : 1f));
            }

            if (movementSpring > 0f && m_TotalDelta.x == 0f && m_TotalDelta.y == 0f) {
                x = Mathf.Lerp(x, 0f, Time.deltaTime * movementSpring);
                y = Mathf.Lerp(y, 0f, Time.deltaTime * movementSpring);
            }

            movement = new Vector2(x, y);
        }
    }

}