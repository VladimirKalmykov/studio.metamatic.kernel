using UnityEngine.Events;

namespace Metamatic.UI {
    using UnityEngine;

    [RequireComponent(typeof(Swipe))]
    public class SwipeSlider : MonoBehaviour
    {
        #region Fields
        public Slides slides;
        [Range(0f, 1f)]
        public float inertness = 0.5f;
        public float magnetism = 5f;
        [SerializeField] int startIndex = 0;
        #endregion
        
        #region Events

        public UnityEvent<int> OnActiveIndexChanged = new UnityEvent<int>();
        #endregion

        #region Private properties
        private Swipe swipe;
        private Vector2 magnitudeRatio;
        private Vector2 positionRatio = Vector2.zero;
        private int m_Index = 0;
        #endregion

        void Awake()
        {
            swipe = GetComponent<Swipe>();
        }

        void Start()
        {
            var normalizedStartIndex = Mathf.Clamp(startIndex, 0, slides.count - 1);
            m_Index = normalizedStartIndex;
            positionRatio = slides.orientation == Slides.Orientation.HORIZONTAL ? new Vector2((slides.count - 1 - normalizedStartIndex) * -1, 0) : new Vector2(0, (slides.count - 1 - normalizedStartIndex) * -1);
            OnActiveIndexChanged.Invoke(normalizedStartIndex);
        }

        // Update is called once per frame
        void Update()
        {
            magnitudeRatio = swipe.isPressed ? swipe.magnitudeRatio : (inertness > 0f ? Vector2.Lerp(magnitudeRatio, Vector2.zero, Time.deltaTime * (1 - inertness) * 10f) : Vector2.zero);
            
            positionRatio += magnitudeRatio * Time.deltaTime;
            var intPosiiton = ClampVector2BySlides(Vector2Int.RoundToInt(positionRatio));
            var nextIndex = (int)(slides.orientation == Slides.Orientation.HORIZONTAL ? positionRatio.x : positionRatio.y);
            if (nextIndex != m_Index)
            {
                OnActiveIndexChanged.Invoke(nextIndex);
                m_Index = nextIndex;
            }
            if (!swipe.isPressed) {
                positionRatio = Vector2.Lerp(positionRatio, intPosiiton, Time.deltaTime * magnetism);
            }
            
            slides.positionRatio = slides.orientation == Slides.Orientation.HORIZONTAL ? new Vector2(positionRatio.x, 0) : new Vector2(0, positionRatio.y);
        }
        
        

        Vector2 ClampVector2BySlides(Vector2 position) {
            return new Vector2(
                Mathf.Clamp(position.x, (slides.count - 1) * -1, 0f),
                Mathf.Clamp(position.y, (slides.count - 1) * -1, 0f)
            );
        }
    }
}
