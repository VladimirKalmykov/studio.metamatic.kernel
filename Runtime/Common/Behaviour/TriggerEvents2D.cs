using System;
using System.Linq;

namespace Metamatic
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using Sirenix.OdinInspector;

    public abstract class TriggerEvents2D<TNormalized> : MonoBehaviour
    {
        public class TriggeringInfo
        {
            public readonly Collider2D collider;
            public readonly float enterTime;
            public readonly TNormalized normalized;

            public TriggeringInfo(Collider2D collider, TNormalized normalized)
            {
                this.collider = collider;
                enterTime = Time.time;
                this.normalized = normalized;
            }
        }

        [SerializeField] public float frequency = 0.5f;
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

        public UnityEvent OnChange = new UnityEvent();

        public TriggerEventsFilteringType filterType = TriggerEventsFilteringType.Every;
        [ShowIf("@this.filterType == TriggerEventsFilteringType.Tagged"), Tooltip("Capture trigger events only for objects with these tags")]
        public Tags tags;

        [ShowIf("@this.filterType == TriggerEventsFilteringType.Explicit"), Tooltip("Capture trigger events only for these objects")]
        public List<GameObject> targets = new List<GameObject>();

        [BoxGroup("Debug"), ShowInInspector, ReadOnly]
        public Dictionary<Collider2D, TriggeringInfo> visitors = new Dictionary<Collider2D, TriggeringInfo>();
        private Collider2D m_Collider;
        private void Awake()
        {
            m_Collider = GetComponentInChildren<Collider2D>();
            if (m_Collider == null) {
                throw new System.Exception("TriggerEvents must have collider");
            }
            if (!m_Collider.isTrigger) {
                throw new System.Exception("TriggerEvents must have trigger collider");
            }
        }

        protected bool InternalValidateCollider(Collider2D collider)
        {
            switch (filterType)
            {
                case TriggerEventsFilteringType.Explicit:
                    if (!targets.Any(go => go != null && collider.transform.IsChildOf(go.transform)))
                    {
                        return false;
                    }
                    break;
                case TriggerEventsFilteringType.Tagged:
                    if (!tags.Contains(collider.transform.gameObject.tag))
                    {
                        return false;
                    }
                    break;
                case TriggerEventsFilteringType.Every:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return true;
        }

        protected abstract bool Validate(Collider2D collider, out TNormalized normalized);
        protected virtual void OnStay(TriggeringInfo info, float stepDuration)
        {
    
        }

        private void OnTriggerEnter2D(Collider2D collider)
        {
            if (visitors.ContainsKey(collider))
            {
                return;
            }
            // if (enteredColliders.Contains(collider))
            // {
            //     return;
            // }

            var normalized = default(TNormalized);
            if (!(InternalValidateCollider(collider) && Validate(collider, out normalized))) return;
            visitors[collider] = new TriggeringInfo(collider, normalized);
            // enteredColliders.Add(collider);
            OnChange.Invoke();
            if (visitors.Count == 1) {
                OnEnter.Invoke();
            }
            
        }

        private float m_NextCheckTime;
        private void FixedUpdate()
        {
            if (visitors.Count == 0 && !(Time.time > m_NextCheckTime)) return;
            m_NextCheckTime = Time.time + frequency;
            var colliderBounds = m_Collider.bounds;

            foreach(var value in visitors.Values.ToArray())
            {
                if (value.collider.gameObject.activeInHierarchy)
                {
                    var stepDuration = Mathf.Min(Time.time - value.enterTime, frequency);
                    OnStay(value, stepDuration);
                    if (value.collider.bounds.Intersects(colliderBounds))
                    {
                        continue;
                    }
                }
                visitors.Remove(value.collider);
                
                OnChange.Invoke();
                if (visitors.Count == 0) {
                    OnLeave?.Invoke();
                }
            }
        }
    }

}