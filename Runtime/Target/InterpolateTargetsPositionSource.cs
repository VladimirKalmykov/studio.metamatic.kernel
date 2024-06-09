using JetBrains.Annotations;

namespace Metamatic
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    
    /// <summary>
    /// Allows to interpolate between multiple targets position. For example, a bird fly to target (SetTarget), in the middle of the way, the target is changed to another target, the bird will interpolate to the new target.
    /// </summary>
    [ExecuteAlways]
    public class InterpolateTargetsPositionSource : TargetPositionSource, ISerializationCallbackReceiver
    {
        public float transitionDuration = 1f;
        [SerializeField] private EasingFunction.Ease defaultEasing = EasingFunction.Ease.EaseInQuad;
        #if UNITY_EDITOR
        [SerializeField] private Color debugColor;
        #endif

        public EasingFunction.Ease easingFunction
        {
            set
            {
                defaultEasing = value;
                BuildEasingFunction();
            }
        }
        
        private Vector3 m_VirtualPosition;

        private class WeightyPoint
        {
            [CanBeNull] public IPositionSource source;
            public float weight;
            public float startTime;

            public WeightyPoint(IPositionSource source, float weight = 0f)
            {
                this.source = source;
                this.weight = weight;
                this.startTime = Time.time;
            }

            public void ResetTime()
            {
                this.startTime = Time.time;
            }
        }
        
        private List<WeightyPoint> m_Points = new List<WeightyPoint>();

        private IPositionSource m_SinglePoint;
        private EasingFunction.Function m_EasingFunction;

        private void Awake()
        {
            m_SinglePoint = new TransformPositionSource(transform);
        }

        private void BuildEasingFunction()
        {
            m_EasingFunction = EasingFunction.GetEasingFunction(defaultEasing);
        }        

        private void Update()
        {
            /* Remove all null-value targets that can be destroyed out of this component logic */
            m_Points.RemoveAll(item => item.source == null);
            if (m_Points.Count < 1) return;
            /* Update weight */
            for (var i = 0; i < m_Points.Count - 1; i++)
            {
                var point = m_Points[i];
                point.weight = m_EasingFunction(point.weight, 0f, Mathf.Clamp01((Time.time - point.startTime) / transitionDuration));
                if (point.weight < 0.001f)
                {
                    m_Points[i].source = null;
                }
            }

            var lastPoint = m_Points[m_Points.Count - 1];
            lastPoint.weight = m_EasingFunction(lastPoint.weight, 1f, Mathf.Clamp01((Time.time - lastPoint.startTime) / transitionDuration));
            
            /* Remove all null-value targets that was marked to be deleted here */
            m_Points.RemoveAll(item => item.source == null);

            var sum = m_Points.Sum(point => point.weight);
            m_VirtualPosition = Vector3.zero;

            for (var i = 0; i < m_Points.Count; i++)
            {
                m_VirtualPosition += transform.InverseTransformPoint(m_Points[i].source.position) * (m_Points[i].weight / sum);
            }

            if (m_Points.Count != 1) return;
            enabled = false;
            m_SinglePoint = m_Points[0].source;
        }

        protected override Vector3 GetPosition()
        {
            return enabled ? transform.TransformPoint(m_VirtualPosition) : m_SinglePoint.position;
        }

        public override void SetTarget(Transform target)
        {
            if (m_Points.Count > 0)
            {
                m_Points[m_Points.Count - 1].ResetTime();
            }
            m_Points.Add(new WeightyPoint(new TransformPositionSource(target), m_Points.Count == 0 ? 1f : 0f));
            if (m_Points.Count > 1)
            {
                enabled = true;
            }
        }
        
        public override void SetTarget(IPositionSource source)
        {
            if (m_Points.Count > 0)
            {
                m_Points[m_Points.Count - 1].ResetTime();
            }
            m_Points.Add(new WeightyPoint(source, m_Points.Count == 0 ? 1f : 0f));
            if (m_Points.Count > 1)
            {
                enabled = true;
            }
        }

        public override IPositionSource GetTarget()
        {
            return null;
        }

        /// <summary>
        /// Set immediate target without interpolation
        /// </summary>
        /// <param name="target"></param>
        public void SetImmediateTarget(Transform target)
        {
            var transformPositionSource = new TransformPositionSource(target);
            m_Points.Clear();
            m_Points.Add(new WeightyPoint(transformPositionSource, 1f));
            enabled = false;
            m_SinglePoint = transformPositionSource;
        }
        
        public void SetImmediateTarget(IPositionSource target)
        {
            m_Points.Clear();
            m_Points.Add(new WeightyPoint(target, 1f));
            enabled = false;
            m_SinglePoint = target;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Handles.color = new Color(1f, 0.22f, 0.19f, 0.4f);
            foreach (var point in m_Points)
            {
                Handles.DrawSolidArc(point.source.position, Vector3.forward, Vector3.up, point.weight * 360f,
                    HandleUtility.GetHandleSize(transform.position) / 5f);
                Handles.DrawSolidDisc(point.source.position, Vector3.forward,
                    HandleUtility.GetHandleSize(transform.position) / 10f);
            }

            Handles.color = debugColor;
            Handles.DrawSolidDisc(m_VirtualPosition, Vector3.forward,
                HandleUtility.GetHandleSize(m_VirtualPosition) / 10f);
        }

        private void OnValidate()
        {
            BuildEasingFunction();
        }
        #endif
        public void OnBeforeSerialize()
        {
            
        }

        public void OnAfterDeserialize()
        {
            BuildEasingFunction();
        }
    }

}