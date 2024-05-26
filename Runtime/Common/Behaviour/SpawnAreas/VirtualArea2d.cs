using System;
using System.Collections.Generic;
using System.Threading;
using Metamatic.Actions;
using Metamatic.DevTools;
using Metamatic.MicroBehaviours;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Metamatic
{
    public abstract class VirtualArea2d: VirtualArea
    {
        
        public enum GeneratePointsType
        {
            Random,
            Strict
        }
        [SerializeField] public float gizmoSize = 0.05f;
        [SerializeField] public List<Vector2> points;
        public bool overlapped { get; set; }
        /// <summary>
        /// Area size of the shape of the area
        /// </summary>
        public abstract float size { get; }
        /// <summary>
        /// Get random position on the area
        /// </summary>
        /// <returns></returns>
        public abstract Vector2 GetRandomPosition(bool local = false);
        /// <summary>
        /// Get random position on the area that is inside the given rect
        /// </summary>
        /// <param name="otherRect"></param>
        /// <returns></returns>
        public abstract Vector2 GetRandomPosition(Rect otherRect);
        /// <summary>
        /// Get random points inside the intersection of the area and the given rect
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="minDistance"></param>
        /// <param name="maxCount"></param>
        /// <param name="spawnedObjects"></param>
        /// <returns></returns>
        public abstract Vector2[] GetSpawnPoints(Rect rect, float minDistance, int maxCount, List<Vector2> spawnedObjects);
        /// <summary>
        /// If the area overlaps with the given rect
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public abstract bool Overlaps(Rect rect);
        public abstract float OverlapsAreaSize(Rect rect);
        public abstract Vector2[] GeneratePoints(int count, GeneratePointsType algorithm = GeneratePointsType.Random);
        
        public event CameraFOVTracker2d.TrackAreaHandler OnCameraFOVIntersectionChange = delegate { };
        
        #if UNITY_EDITOR
        [Button]
        public void PreparePoints(int count, GeneratePointsType algorithm = GeneratePointsType.Random)
        {
            if (points == null)
            {
                points = new List<Vector2>();
            }
            points.Clear();
            var nextPoints = GeneratePoints(count, algorithm);
            /* Convert world points to local points */
            foreach (var point in nextPoints)
            {
                points.Add(transform.InverseTransformPoint(point));
            }
        }
        #endif

        public virtual void OnDrawGizmos()
        {
            
        }


        private CancellationTokenSource m_DisableCancellationTokenSource;
        private async void OnEnable()
        {
            m_DisableCancellationTokenSource = new CancellationTokenSource();
            var token = m_DisableCancellationTokenSource.Token;
            if (autoTrack)
            {
                var fovManager = await Dispatcher.global.WhenRegistered<FOVManager2d>(token);
                using var tracker = fovManager.TrackArea(this, CameraFOVTracker2d.FovBoundsType.Full | CameraFOVTracker2d.FovBoundsType.Fov, (intersected, intersection) =>
                {
                    OnCameraFOVIntersectionChange(intersected, intersection);
                });
            }
            
            
            await token;
        }

        private void OnDisable()
        {
            m_DisableCancellationTokenSource?.Cancel();
        }
    }
}