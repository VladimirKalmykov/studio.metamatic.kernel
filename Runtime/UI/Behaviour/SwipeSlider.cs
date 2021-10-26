namespace Metamatic.UI {
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Metamatic.DevTools;

    [RequireComponent(typeof(Swipe))]
    public class SwipeSlider : MonoBehaviour
    {
        #region Fields
        public Slides slides;
        [Range(0f, 1f)]
        public float inertness = 0.5f;
        public float magnetism = 5f;
        #endregion

        #region Private properties
        private Swipe swipe;
        private Vector2 magnitudeRatio;
        private Vector2 positionRatio = Vector2.zero;
        #endregion

        void Awake()
        {
            swipe = GetComponent<Swipe>();
        }

        // Update is called once per frame
        void Update()
        {
            magnitudeRatio = swipe.isPressed ? swipe.magnitudeRatio : (inertness > 0f ? Vector2.Lerp(magnitudeRatio, Vector2.zero, Time.deltaTime * (1 - inertness) * 10f) : Vector2.zero);
            
            positionRatio += magnitudeRatio * Time.deltaTime;
            if (!swipe.isPressed) {
                positionRatio = Vector2.Lerp(positionRatio, ClampVector2BySlides(Vector2Int.RoundToInt(positionRatio)), Time.deltaTime * magnetism);
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
