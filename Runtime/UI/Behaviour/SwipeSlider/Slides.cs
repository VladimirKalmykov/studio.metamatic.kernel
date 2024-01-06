using System;
using System.Linq;

namespace Metamatic.UI {
    using UnityEngine;
    using Sirenix.OdinInspector;

    public class Slides : MonoBehaviour
    {
        public enum Orientation {
            HORIZONTAL,
            VERTICAL
        }
        
        #region Events

        public event Action OnSlidesChanged;
        #endregion
        
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
                return m_CachedChildren.Length;
            }
        }
        #endregion

        #region Private properties

        private RectTransform m_Rect;
        private Transform[] m_CachedChildren = new Transform[0];
        #endregion

        private void Awake() {
            m_Rect = (RectTransform)transform;
        }

        private void Start() {
            NormalizeChildren();
        }

        private void Update() {
            size = new Vector2(m_Rect.rect.width, m_Rect.rect.height);

            m_Rect.offsetMin = positionRatio * size;
            m_Rect.offsetMax = positionRatio * size;
        }

        private void FixedUpdate() {
            if (GetChildrenHaveChanged()) {
                NormalizeChildren();
            }
        }

        /// <summary>
        /// Checks if children have changed
        /// </summary>
        /// <returns></returns>
        private bool GetChildrenHaveChanged()
        {
            return m_CachedChildren.Length != transform.childCount || m_CachedChildren.Where((t, i) => t != transform.GetChild(i)).Any();
        }

        private void NormalizeChildren() {
            var index = 0;
            m_CachedChildren = new Transform[transform.childCount];
            foreach(RectTransform child in transform) {
                child.anchoredPosition = Vector2.zero;
                child.sizeDelta = Vector2.zero;
                if (orientation == Orientation.HORIZONTAL) {
                    child.anchorMin = new Vector2(index, 0);
                    child.anchorMax = new Vector2(index + 1, 1);
                } else {
                    child.anchorMin = new Vector2(0, transform.childCount - index - 1);
                    child.anchorMax = new Vector2(1, transform.childCount - index);
                }
                child.pivot = Vector2.zero;
                m_CachedChildren[index] = child as Transform;
                index++;
            }
            OnSlidesChanged?.Invoke();
        }

        #if UNITY_EDITOR
        private void OnValidate() {
            NormalizeChildren();
        }
        #endif
    }
}