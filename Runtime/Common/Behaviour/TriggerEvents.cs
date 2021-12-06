using System;
using System.Linq;

namespace Metamatic
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using Sirenix.OdinInspector;

    public class TriggerEvents : MonoBehaviour
    {
        public enum FilteringType
        {
            Every,
            Tagged,
            Explicit
        }
        /// <summary>
        /// Wlll trigger when at least one collider did enter trigger
        /// </summary>
        /// <returns></returns>
        // ReSharper disable once InconsistentNaming
        public UnityEvent OnEnter = new UnityEvent();
        /// <summary>
        /// Will trigger when no colliders stay within the trigger
        /// </summary>
        /// <returns></returns>
        // ReSharper disable once InconsistentNaming
        public UnityEvent OnLeave = new UnityEvent();

        public FilteringType filterType = FilteringType.Every;
        [ShowIf("@this.filterType == FilteringType.Tagged"), Tooltip("Capture trigger events only for objects with these tags")]
        public Tags tags;

        [ShowIf("@this.filterType == FilteringType.Explicit"), Tooltip("Capture trigger events only for these objects")]
        public List<GameObject> targets = new List<GameObject>();
        
        [BoxGroup("Debug"), ShowInInspector, ReadOnly]
        public List<Collider> enteredColliders = new List<Collider>();
        private Collider m_Collider;
        private void Awake()
        {
            m_Collider = GetComponentInChildren<Collider>();
            if (m_Collider == null) {
                throw new System.Exception("TriggerEvents must have collider");
            }
            if (!m_Collider.isTrigger) {
                throw new System.Exception("TriggerEvents must have trigger collider");
            }
        }

        private void OnTriggerEnter(Collider collider)
        {
            if (enteredColliders.Contains(collider))
            {
                return;
            }
            switch (filterType)
            {
                case FilteringType.Explicit:
                    if (!targets.Any(go => go != null && collider.transform.IsChildOf(go.transform)))
                    {
                        return;
                    }
                    break;
                case FilteringType.Tagged:
                    if (!tags.Contains(collider.transform.gameObject.tag))
                    {
                        return;
                    }
                    break;
                case FilteringType.Every:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            enteredColliders.Add(collider);
            if (enteredColliders.Count == 1) {
                OnEnter.Invoke();
            }
            
        }

        private float m_NextCheckTime;

        private void FixedUpdate()
        {
            if (!(Time.time > m_NextCheckTime)) return;
            m_NextCheckTime = Time.time + 1f;
            var colliderBounds = m_Collider.bounds;

            foreach(var enteredCollider in enteredColliders.ToArray())
            {
                if (enteredCollider.gameObject.activeInHierarchy &&
                    enteredCollider.bounds.Intersects(colliderBounds)) continue;
                enteredColliders.Remove(enteredCollider);

                if (enteredColliders.Count == 0) {
                    OnLeave?.Invoke();
                }
            }
        }
    }

}