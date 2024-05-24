using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.MicroBehaviours
{
    /// <summary>
    /// MicroBehaviour that tracks spawn areas and emit events when they are inside the camera field of view
    /// </summary>
    public class CameraFOVTracker2d: MicroBehaviour
    {
        private class AreaTracker
        {
            public readonly VirtualArea2d area;
            public int acquirings = 0;
            /// <summary>
            /// Activator for the area (null if not activated)
            /// </summary>
            public VirtualArea2d.Activator activator;
            /// <summary>
            /// Refers to the active rect of camera FOV which is currently overlaps area
            /// </summary>
            public Rect activeRect;
            public AreaTracker(VirtualArea2d area)
            {
                this.area = area;
            }
            
            public void Acquire()
            {
                acquirings++;
            }
            public void Release()
            {
                acquirings--;
            }
        }
        private Camera m_CustomCamera;
        private float m_InnerBuffer;
        private float m_OuterBuffer;
        private Dictionary<VirtualArea2d, AreaTracker> m_TrackedAreas;
        private List<Transform> m_TrackedTransforms;
        private float m_ZDepth;
        private Rect[] m_SideRects;

        public float innerBuffer
        {
            get => m_InnerBuffer;
            set => m_InnerBuffer = value;
        }

        public float outerBuffer
        {
            get => m_OuterBuffer;
            set => m_OuterBuffer = value;
        }

        private IReadOnlyDictionary<VirtualArea2d, AreaTracker> trackedAreas
        {
            get => m_TrackedAreas;
        }

        public IReadOnlyList<Transform> trackedTransforms
        {
            get => m_TrackedTransforms;
        }

        public float zDepth
        {
            get => m_ZDepth;
            set => m_ZDepth = value;
        }
        private bool m_IsTracking = false;
        
        #region Events
        public event Action<VirtualArea2d, Rect> OnAreaActivated;
        public event Action<VirtualArea2d> OnAreaDisactivated;
        public event Action<Transform> OnTransformOverlapped;
        public event Action<Transform> OnTransformNotOverlapped;
        public event Action OnPostTracking;
        
        public DisposableAction SubscribeOnAreaOverlapped(Action<VirtualArea2d, Rect> action)
        {
            OnAreaActivated += action;
            return new DisposableAction(() => OnAreaActivated -= action);
        }
        public DisposableAction SubscribeOnAreaNotOverlapped(Action<VirtualArea2d> action)
        {
            OnAreaDisactivated += action;
            return new DisposableAction(() => OnAreaDisactivated -= action);
        }
        public DisposableAction SubscribeOnTransformOverlapped(Action<Transform> action)
        {
            OnTransformOverlapped += action;
            return new DisposableAction(() => OnTransformOverlapped -= action);
        }
        public DisposableAction SubscribeOnTransformNotOverlapped(Action<Transform> action)
        {
            OnTransformNotOverlapped += action;
            return new DisposableAction(() => OnTransformNotOverlapped -= action);
        }
        #endregion
        
        public Camera camera
        {
            get
            {
                if (m_CustomCamera == null)
                {
                    m_CustomCamera = Camera.main;
                }

                return m_CustomCamera;
            }
            set => m_CustomCamera = value;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="camera">Current camera (by default Camera.main)</param>
        /// <param name="innerBuffer">Gap around FOV that considered as visible for camera as well</param>
        /// <param name="outerBuffer">Second gap around inner gap is used to overlap with spawn areas (because they should be activated outside the FOV but close to it)</param>
        /// <param name="zDepth">Distance from screen and tracked plane z depth</param>
        /// <param name="trackAreas">Array of areas for tracking</param>
        /// <param name="trackedTransforms">Array of transforms for tracking (unlike the areas all transforms are tracked within outer buffer full size)</param>
        /// <example>
        /// await using var activator = new CameraFOVBufferSpawnAreasActivator2d(Camera.main, 0.1f, 0.2f, 0f, new SpawnArea2d[]{new RectSpawnArea2d(new Rect(0, 0, 1, 1))});
        /// </example>
        public CameraFOVTracker2d(
            Camera camera = null,
            float innerBuffer = 0f,
            float outerBuffer = 1f,
            float zDepth = 0f,
            bool autoRun = true
            ): base()
        {
            m_CustomCamera = camera;
            this.m_InnerBuffer = innerBuffer;
            this.m_OuterBuffer = outerBuffer;
            this.m_TrackedAreas = new Dictionary<VirtualArea2d, AreaTracker>();
            this.m_TrackedTransforms = new List<Transform>();
            this.m_ZDepth = zDepth;

            if (autoRun)
            {
                Run();
            }
        }
        
        public bool ContainsArea(VirtualArea2d area)
        {
            return m_TrackedAreas.ContainsKey(area);
        }
        
        public void TrackArea(VirtualArea2d area)
        {
            SafeExecute(() =>
            {
                if (m_TrackedAreas.TryGetValue(area, out var tracker))
                {
                    tracker.Acquire();
                    return;
                }
                m_TrackedAreas.Add(area, new AreaTracker(area));
            });
        }
        
        public void UntrackArea(VirtualArea2d area)
        {
            SafeExecute(() =>
            {
                if (m_TrackedAreas.TryGetValue(area, out var tracker))
                {
                    tracker.Release();
                    if (tracker.acquirings == 0)
                    {
                        m_TrackedAreas.Remove(area);
                    }
                }
            });
        }
        
        public void TrackAreas(IEnumerable<VirtualArea2d> areas)
        {
            SafeExecute(() =>
            {
                foreach (var area in areas)
                {
                    TrackArea(area);
                }
            });
        }
        
        public bool HasObject(Transform t)
        {
            return m_TrackedTransforms.Contains(t);
        }
        
        public void TrackObject(Transform t)
        {
            SafeExecute(() =>
            {
                if (m_TrackedTransforms.Contains(t)) return;
                m_TrackedTransforms.Add(t);
            });
        }
        
        public void UntrackObject(Transform t)
        {
            SafeExecute(() =>
            {
                if (!m_TrackedTransforms.Contains(t)) return;
                m_TrackedTransforms.Remove(t);
            });
        }
        
        public void TrackObjects(IEnumerable<Transform> transforms)
        {
            SafeExecute(() =>
            {
                if (m_TrackedTransforms.Count == 0)
                {
                    m_TrackedTransforms.AddRange(transforms);
                    return;
                }
                foreach (var t in transforms)
                {
                    TrackObject(t);
                }
            });
        }
        
        
        /// <summary>
        /// Execute action safely (when not tracking)
        /// </summary>
        /// <param name="action"></param>
        public void SafeExecute(Action action)
        {
            if (!m_IsTracking)
            {
                action();
            }
            else
            {
                OnPostTracking += action;
            }
        }
        
        #region Private Methods

        protected override async UniTask Main(CancellationToken cancellationToken)
        {
            Rect previousBounds = default;
            var diceThrower = new UniqueDice(4);
            using var lifecycle = AsyncUtils.PlayerLoopAction(() =>
            {
                m_IsTracking = true;
                var (mainBounds, innerBufferBounds) = CalculateCameraBounds(this.camera, m_ZDepth, outerBuffer, innerBuffer);
                if (previousBounds == mainBounds) return;
                
                m_SideRects = new Rect[]
                {
                    new Rect(mainBounds.xMin, mainBounds.yMin, innerBufferBounds.xMin - mainBounds.xMin,
                        mainBounds.height), // lEFT
                    new Rect(innerBufferBounds.xMax, mainBounds.yMin, mainBounds.xMax - innerBufferBounds.xMax,
                        mainBounds.height), // RIGHT
                    new Rect(mainBounds.xMin, innerBufferBounds.yMax, mainBounds.width,
                        mainBounds.yMax - innerBufferBounds.yMax), // TOP
                    new Rect(mainBounds.xMin, mainBounds.yMin, mainBounds.width,
                        innerBufferBounds.yMin - mainBounds.yMin) // BOTTOM
                };
                    
                if (m_TrackedAreas != null) {
                    /* Track areas */
                    foreach (var (area, areaTrack) in m_TrackedAreas)
                    {
                        diceThrower.Reset();
                        int side = 0;
                        while((side = diceThrower.Next()) > -1)
                        {
                            var sideRect = m_SideRects[side];
                            var overlaps = area.Overlaps(sideRect);
                            if (overlaps)
                            {
                                if (areaTrack.activator == null)
                                {
                                    areaTrack.activator = new VirtualArea2d.Activator((pos) => areaTrack.activeRect.Contains(pos));
                                    area.Activate(areaTrack.activator);
                                }
                                else
                                {
                                    areaTrack.activeRect = sideRect;
                                    areaTrack.activator.TriggerIntersection();
                                }
                                OnAreaActivated?.Invoke(area, sideRect);
                                break;
                            }
                            else
                            {
                                if (areaTrack.activator != null)
                                {
                                    area.Deactivate(areaTrack.activator);
                                    areaTrack.activator = null;
                                }
                                OnAreaDisactivated?.Invoke(area);
                            }
                        }
                    }
                }
                
                if (m_TrackedTransforms != null)
                {
                    /* Track transforms */
                    foreach (var t in m_TrackedTransforms)
                    {
                        if (mainBounds.Contains(t.position))
                        {
                            OnTransformOverlapped?.Invoke(t);
                        }
                        else
                        {
                            OnTransformNotOverlapped?.Invoke(t);
                        }
                    }
                }
                m_IsTracking = false;
                OnPostTracking?.Invoke();
                OnPostTracking = null;
            }, PlayerLoopTiming.FixedUpdate);

            await cancellationToken;
        }
        
        private (Rect mainBounds, Rect innerBufferBounds) CalculateCameraBounds(Camera camera, float zDepth, float outerBuffer, float innerBuffer)
        {
            Vector3 bottomLeft = camera.ViewportToWorldPoint(new Vector3(0, 0, Mathf.Abs(camera.transform.position.z - zDepth)));
            Vector3 topRight = camera.ViewportToWorldPoint(new Vector3(1, 1, Mathf.Abs(camera.transform.position.z - zDepth)));

            Rect mainBounds = new Rect(
                bottomLeft.x - outerBuffer - innerBuffer,
                bottomLeft.y - outerBuffer - innerBuffer,
                (topRight.x - bottomLeft.x) + (outerBuffer + innerBuffer) * 2,
                (topRight.y - bottomLeft.y) + (outerBuffer + innerBuffer) * 2
            );

            Rect innerBufferBounds = new Rect(
                bottomLeft.x - innerBuffer,
                bottomLeft.y - innerBuffer,
                (topRight.x - bottomLeft.x) + innerBuffer * 2,
                (topRight.y - bottomLeft.y) + innerBuffer * 2
            );

            return (mainBounds, innerBufferBounds);
        }
        #endregion
        
        #if UNITY_EDITOR
        public void DrawGizmos()
        {
            if (camera != null)
            {
                var (mainBounds, bufferBounds) = CalculateCameraBounds(camera, zDepth, outerBuffer, innerBuffer);
                Gizmos.color = Color.green;
                Gizmos.DrawLine(new Vector3(mainBounds.xMin, mainBounds.yMin, zDepth), new Vector3(mainBounds.xMax, mainBounds.yMin, zDepth));
                Gizmos.DrawLine(new Vector3(mainBounds.xMax, mainBounds.yMin, zDepth), new Vector3(mainBounds.xMax, mainBounds.yMax, zDepth));
                Gizmos.DrawLine(new Vector3(mainBounds.xMax, mainBounds.yMax, zDepth), new Vector3(mainBounds.xMin, mainBounds.yMax, zDepth));
                Gizmos.DrawLine(new Vector3(mainBounds.xMin, mainBounds.yMax, zDepth), new Vector3(mainBounds.xMin, mainBounds.yMin, zDepth));

                Gizmos.color = Color.blue;
                Gizmos.DrawLine(new Vector3(bufferBounds.xMin, bufferBounds.yMin, zDepth), new Vector3(bufferBounds.xMax, bufferBounds.yMin, zDepth));
                Gizmos.DrawLine(new Vector3(bufferBounds.xMax, bufferBounds.yMin, zDepth), new Vector3(bufferBounds.xMax, bufferBounds.yMax, zDepth));
                Gizmos.DrawLine(new Vector3(bufferBounds.xMax, bufferBounds.yMax, zDepth), new Vector3(bufferBounds.xMin, bufferBounds.yMax, zDepth));
                Gizmos.DrawLine(new Vector3(bufferBounds.xMin, bufferBounds.yMax, zDepth), new Vector3(bufferBounds.xMin, bufferBounds.yMin, zDepth));
            
                /* Draw side rects as filled rectangles */
                var colors = new Color[]
                {
                    Color.red,
                    Color.blue,
                    Color.green,
                    Color.yellow
                };
                for (var i = 0; i < m_SideRects.Length; i++)
                {
                    Gizmos.color = colors[i];
                    var sideRect = m_SideRects[i];
                    Gizmos.DrawCube(new Vector3(sideRect.center.x, sideRect.center.y, zDepth), new Vector3(sideRect.width, sideRect.height, 0));
                }
            }
        }
        #endif
    }
}