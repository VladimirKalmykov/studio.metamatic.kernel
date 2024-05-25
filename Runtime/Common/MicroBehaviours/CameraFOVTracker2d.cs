using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.MicroBehaviours
{
    /// <summary>
    /// MicroBehaviour that tracks spawn areas and emit events when they are inside the camera field of view
    /// </summary>
    public class CameraFOVTracker2d: MicroBehaviour
    {
        [Flags]
        public enum BoundsType
        {
            None = 0,
            /// <summary>
            /// All bounds including FOV, inner buffer and outer buffer
            /// </summary>
            All = 1,
            /// <summary>
            /// Only FOV bounds (what camera sees)
            /// </summary>
            Fov = 2,
            /// <summary>
            /// FOV plus inner buffer
            /// </summary>
            InnerBuffer = 4,
            /// <summary>
            /// Outer left plus outer right plus outer top plus outer bottom
            /// </summary>
            OuterBufferOnly = 8,
            /// <summary>
            /// Left part of outer buffer
            /// </summary>
            OuterLeft = 16,
            /// <summary>
            /// Right part of outer buffer
            /// </summary>
            OuterRight = 32,
            /// <summary>
            /// Top part of outer buffer
            /// </summary>
            OuterTop = 64,
            /// <summary>
            /// Bottom part of outer buffer
            /// </summary>
            OuterBottom = 128
        }
        
        private static readonly Dictionary<BoundsType, int[]> MapBoundsTypeToBoundsCacheIndex = new()
        {
            {BoundsType.All, new[]{0}},
            {BoundsType.Fov, new[]{1}},
            {BoundsType.InnerBuffer, new[]{2}},
            {BoundsType.OuterBufferOnly, new[]{3, 4, 5, 6}},
            {BoundsType.OuterLeft, new[]{3}},
            {BoundsType.OuterRight, new[]{4}},
            {BoundsType.OuterTop, new[]{5}},
            {BoundsType.OuterBottom, new[]{6}}
        };
        
        private class ObjectTracker
        {
            public readonly Transform transform;
            public BoundsType activatedBoundsTypesMask { get; private set; } = 0;
            private Dictionary<BoundsType, int> m_AcquiredBoundsTypeMap = new();
            public BoundsType acquiredBoundsTypesBinaryMask { get; private set; }= 0;
            private event Action<BoundsType> OnActivateChange;
            public int acquireCount { get; private set; }
            public ObjectTracker(Transform t)
            {
                this.transform = t;
            }

            public bool ApplyBounds(BoundsType boundsType)
            {
                if ((activatedBoundsTypesMask & boundsType) == boundsType)
                {
                    return false;
                }
                activatedBoundsTypesMask = boundsType;
                OnActivateChange?.Invoke(activatedBoundsTypesMask);
                return true;
            }
            
            public DisposableAction Acquire(BoundsType boundsTypeMask, Action<Transform, BoundsType> onReactivate = null)
            {
                var boundsTypes = DecomposeBounds(boundsTypeMask);
                acquireCount++;
                foreach (var boundsType in boundsTypes)
                {
                    if (!m_AcquiredBoundsTypeMap.ContainsKey(boundsType))
                    {
                        m_AcquiredBoundsTypeMap.Add(boundsType, 0);
                    }
                    m_AcquiredBoundsTypeMap[boundsType]++;
                }
                
                /* Bake types to easily iterate over acquired bounds types in a loop */
                BakeAcquiredBoundsTypes();
                
                /* If callback is provided */
                Action<BoundsType> handler = null;
                if (onReactivate != null)
                {
                    /* Compare to current activated mask and trigger reactivation */
                    var intersection = activatedBoundsTypesMask & boundsTypeMask;
                    onReactivate.Invoke(transform, intersection);
                    handler = (activeBounds) =>
                    {
                        var nextIntersection = activeBounds & boundsTypeMask;
                        /* Compare if intersection is changed */
                        if ((nextIntersection != 0) != (intersection != 0))
                        {
                            intersection = nextIntersection;
                            onReactivate.Invoke(transform, intersection);
                        }
                    };
                    OnActivateChange += handler;
                }
                
                return new DisposableAction(() =>
                {
                    acquireCount--;
                    OnActivateChange -= handler;
                    foreach (var boundsType in boundsTypes)
                    {
                        if (m_AcquiredBoundsTypeMap.ContainsKey(boundsType))
                        {
                            m_AcquiredBoundsTypeMap[boundsType]--;
                        }
                    }
                    BakeAcquiredBoundsTypes();
                });
            }
            
            private static BoundsType[] DecomposeBounds(BoundsType bounds)
            {
                List<BoundsType> result = new List<BoundsType>();

                foreach (BoundsType value in Enum.GetValues(typeof(BoundsType)))
                {
                    if (value != BoundsType.None && (bounds & value) == value)
                    {
                        result.Add(value);
                    }
                }

                return result.ToArray();
            }

            private void BakeAcquiredBoundsTypes()
            {
                acquiredBoundsTypesBinaryMask = 0;
                foreach (var (boundsType, count) in m_AcquiredBoundsTypeMap)
                {
                    if (count > 0)
                    {
                        acquiredBoundsTypesBinaryMask |= boundsType;
                    }
                }
            }
        }
        private class AreaTracker
        {
            public readonly VirtualArea2d area;
            public int acquirings;
            /// <summary>
            /// Activator for the area (null if not activated)
            /// </summary>
            public VirtualArea2d.Activator activator { get; private set; }
            /// <summary>
            /// Refers to the active rect of camera FOV which is currently overlaps area
            /// </summary>
            public Rect activeRect { get; private set; }

            private Action m_DisposeHandler;
            private event Action<VirtualArea2d.Activator> OnAreaActivated;
            public AreaTracker(VirtualArea2d area)
            {
                this.area = area;
            }
            public void Activate(VirtualArea2d.Activator nextActivator)
            {
                this.activator = nextActivator;
                nextActivator.SubscribeDeactivation(() =>
                {
                    this.activator = null;
                });
                OnAreaActivated?.Invoke(nextActivator);
            }
            public void Acquire(Action<VirtualArea2d.Activator> onAreaActivated = null)
            {
                acquirings++;
                if (onAreaActivated != null)
                {
                    OnAreaActivated += onAreaActivated;
                }
            }
            public void Release(Action<VirtualArea2d.Activator> onAreaActivated = null)
            {
                acquirings--;
                if (onAreaActivated != null)
                {
                    OnAreaActivated -= onAreaActivated;
                }
            }
            
            public void SetIntersection(Rect rect)
            {
                if (activeRect != rect)
                {
                    activeRect = rect;
                    activator.TriggerIntersection();
                }
            }
        }
        
        private Camera m_CustomCamera;
        private float m_InnerBuffer;
        private float m_OuterBuffer;
        private Dictionary<VirtualArea2d, AreaTracker> m_TrackedAreas;
        private List<ObjectTracker> m_TrackedObjects;
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

        public float zDepth
        {
            get => m_ZDepth;
            set => m_ZDepth = value;
        }
        private bool m_IsTracking;
        
        #region Events
        public event Action<VirtualArea2d, Rect> OnAreaActivated;
        public event Action<VirtualArea2d> OnAreaDeactivated;
        public event Action<Transform, BoundsType> OnObjectReactivate;
        public event Action OnPostTracking;
        
        public DisposableAction SubscribeOnAreaOverlapped(Action<VirtualArea2d, Rect> action)
        {
            OnAreaActivated += action;
            return new DisposableAction(() => OnAreaActivated -= action);
        }
        public DisposableAction SubscribeOnAreaNotOverlapped(Action<VirtualArea2d> action)
        {
            OnAreaDeactivated += action;
            return new DisposableAction(() => OnAreaDeactivated -= action);
        }
        public DisposableAction SubscribeObjectReactivate(Action<Transform, BoundsType> action)
        {
            OnObjectReactivate += action;
            return new DisposableAction(() => OnObjectReactivate -= action);
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
        /// <param name="autoRun">Run tracking automatically</param>
        /// <example>
        /// await using var activator = new CameraFOVBufferSpawnAreasActivator2d(Camera.main, 0.1f, 0.2f, 0f, new SpawnArea2d[]{new RectSpawnArea2d(new Rect(0, 0, 1, 1))});
        /// </example>
        public CameraFOVTracker2d(
            Camera camera = null,
            float innerBuffer = 0f,
            float outerBuffer = 1f,
            float zDepth = 0f,
            bool autoRun = true
            )
        {
            m_CustomCamera = camera;
            this.m_InnerBuffer = innerBuffer;
            this.m_OuterBuffer = outerBuffer;
            this.m_TrackedAreas = new Dictionary<VirtualArea2d, AreaTracker>();
            this.m_TrackedObjects = new List<ObjectTracker>();
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
        
        /// <summary>
        /// Track area with optional callback on area activated
        /// </summary>
        /// <param name="area"></param>
        /// <param name="onAreaActivated"></param>
        /// <returns></returns>
        public DisposableAction TrackArea(VirtualArea2d area, Action<VirtualArea2d.Activator> onAreaActivated = null)
        {
            var executed = false;
            SafeExecute(() =>
            {
                if (executed) return;
                executed = true;
                if (!m_TrackedAreas.TryGetValue(area, out var tracker))
                {
                    tracker = new AreaTracker(area);
                    m_TrackedAreas.Add(area, tracker);
                }
                tracker.Acquire(onAreaActivated);
                
            });

            return new DisposableAction(() =>
            {
                if (!executed)
                {
                    executed = true;
                    return;
                }
                UntrackArea(area, onAreaActivated);
            });
        }
        
        public void UntrackArea(VirtualArea2d area, Action<VirtualArea2d.Activator> onAreaActivated = null)
        {
            if (m_TrackedAreas.TryGetValue(area, out var tracker))
            {
                SafeExecute(() =>
                {
                    tracker.Release(onAreaActivated);
                    if (tracker.acquirings == 0)
                    {
                        m_TrackedAreas.Remove(area);
                    }
                });
            }
        }
        
        public DisposableAction TrackAreas(IEnumerable<VirtualArea2d> areas, Action<VirtualArea2d.Activator> onAreaActivated = null)
        {
            var disposables = areas.Select(area => TrackArea(area, onAreaActivated)).ToList();
            return new DisposableAction(() =>
            {
                foreach (var disposable in disposables)
                {
                    disposable.Dispose();
                }
            });
        }
        
        public bool HasObject(Transform t)
        {
            /* Check if object is tracked, ensure that it has ObjectTracker with same transform  */
            return m_TrackedObjects.Any(tracker => tracker.transform == t);
        }
        
        public DisposableAction TrackObject(Transform t, BoundsType boundsType = BoundsType.All, Action<Transform, BoundsType> onActivate = null)
        {
            var executed = false;
            /* Create object tracker if not exists */
            ObjectTracker tracker = m_TrackedObjects.FirstOrDefault(tracker => tracker.transform == t);
            if (tracker == null)
            {
                tracker = new ObjectTracker(t);
                SafeExecute(() =>
                {
                    
                    if (executed) return;
                    executed = true;
                    m_TrackedObjects.Add(tracker);
                });
            }
                
            var release = (Action)tracker.Acquire(boundsType, onActivate);

            return new DisposableAction(() =>
            {
                release();
                if (executed)
                {
                    executed = true;
                    m_TrackedObjects.Remove(tracker);
                }
            });
        }
        
        public DisposableAction TrackObjects(IEnumerable<Transform> transforms, BoundsType boundsType = BoundsType.All, Action<Transform, BoundsType> onActivate = null)
        {
            var disposables = transforms.Select(t => TrackObject(t, boundsType, onActivate)).ToList();
            return new DisposableAction(() =>
            {
                foreach (var disposable in disposables)
                {
                    disposable.Dispose();
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
            var diceThrower = new UniqueDice(4);
            Rect[] boundsTypesCache = new Rect[6];
            using var lifecycle = AsyncUtils.PlayerLoopAction(() =>
            {
                m_IsTracking = true;
                var (fovBounds, allBounds, innerBufferBounds) = CalculateCameraBounds(this.camera, m_ZDepth, outerBuffer, innerBuffer);
                
                m_SideRects = new[]
                {
                    new Rect(allBounds.xMin, allBounds.yMin, innerBufferBounds.xMin - allBounds.xMin,
                        allBounds.height), // lEFT
                    new Rect(innerBufferBounds.xMax, allBounds.yMin, allBounds.xMax - innerBufferBounds.xMax,
                        allBounds.height), // RIGHT
                    new Rect(allBounds.xMin, innerBufferBounds.yMax, allBounds.width,
                        allBounds.yMax - innerBufferBounds.yMax), // TOP
                    new Rect(allBounds.xMin, allBounds.yMin, allBounds.width,
                        innerBufferBounds.yMin - allBounds.yMin) // BOTTOM
                };
                
                /* All */
                boundsTypesCache[0] = allBounds;
                /* FOV */
                boundsTypesCache[1] = fovBounds;
                /* Inner buffer */
                boundsTypesCache[2] = innerBufferBounds;
                /* Left */
                boundsTypesCache[3] = new Rect(allBounds.xMin, allBounds.yMin, innerBufferBounds.xMin - allBounds.xMin,
                    allBounds.height);
                /* Right */
                boundsTypesCache[4] = new Rect(innerBufferBounds.xMax, allBounds.yMin, allBounds.xMax - innerBufferBounds.xMax,
                    allBounds.height);
                /* Top */
                boundsTypesCache[5] = new Rect(allBounds.xMin, innerBufferBounds.yMax, allBounds.width,
                    allBounds.yMax - innerBufferBounds.yMax);
                /* Bottom */
                boundsTypesCache[6] = new Rect(allBounds.xMin, allBounds.yMin, allBounds.width,
                    innerBufferBounds.yMin - allBounds.yMin);
                    
                if (m_TrackedAreas != null) {
                    /* Track areas */
                    foreach (var (area, areaTrack) in m_TrackedAreas)
                    {
                        diceThrower.Reset();
                        int side = 0;
                        bool overlaps = false;
                        while((side = diceThrower.Next()) > -1)
                        {
                            var sideRect = m_SideRects[side];
                            overlaps = area.Overlaps(sideRect);
                            if (overlaps)
                            {
                                if (areaTrack.activator == null)
                                {
                                    areaTrack.Activate(new VirtualArea2d.Activator(area, (pos) => areaTrack.activeRect.Contains(pos)));
                                    area.Activate(areaTrack.activator);
                                }
                                else
                                {
                                    areaTrack.SetIntersection(sideRect);
                                }
                                OnAreaActivated?.Invoke(area, sideRect);
                                break;
                            }
                        }

                        if (!overlaps)
                        {
                            if (areaTrack.activator != null)
                            {
                                area.Deactivate(areaTrack.activator);
                                areaTrack.activator.Release();
                            }
                            OnAreaDeactivated?.Invoke(area);
                        }
                    }
                }
                
                if (m_TrackedObjects != null)
                {
                    /* Track transforms */
                    foreach (var t in m_TrackedObjects)
                    {
                        /* t.acquiredBoundsTypesBinaryMask is a binary mask of bounds types that are acquired. Step every possible boundsType and calculate overlapping */
                        var mask = t.acquiredBoundsTypesBinaryMask;
                        BoundsType resultBoundsTypeMask = 0;
                        for (var i = 1; i < 8; i++)
                        {
                            var boundsType = (BoundsType)i;
                            if ((mask & boundsType) != 0)
                            {
                                if (BoundsContainsPoint(boundsTypesCache, MapBoundsTypeToBoundsCacheIndex[boundsType], t))
                                {
                                    resultBoundsTypeMask |= boundsType;
                                }
                                else
                                {
                                    resultBoundsTypeMask &= ~boundsType;
                                }
                            }
                        }

                        if (t.ApplyBounds(resultBoundsTypeMask))
                        {
                            OnObjectReactivate?.Invoke(t.transform, resultBoundsTypeMask);
                        }
                    }
                }
                m_IsTracking = false;
                OnPostTracking?.Invoke();
                OnPostTracking = null;
            }, PlayerLoopTiming.FixedUpdate);

            await cancellationToken;
            /* Deactivate all areas */
            foreach (var (area, areaTrack) in m_TrackedAreas)
            {
                if (areaTrack.activator != null)
                {
                    areaTrack.activator.Release();
                    area.Deactivate(areaTrack.activator);
                }
            }
        }
        
        private bool BoundsContainsPoint(Rect[] boundsTypesCache, int[] boundsTypes, ObjectTracker t)
        {
            foreach (var boundsType in boundsTypes)
            {
                if (boundsTypesCache[boundsType].Contains(t.transform.position))
                {
                    return true;
                }
            }
            return false;
        }
        
        private (Rect fovBounds, Rect mainBounds, Rect innerBufferBounds) CalculateCameraBounds(Camera camera, float zDepth, float outerBuffer, float innerBuffer)
        {
            Vector3 bottomLeft = camera.ViewportToWorldPoint(new Vector3(0, 0, Mathf.Abs(camera.transform.position.z - zDepth)));
            Vector3 topRight = camera.ViewportToWorldPoint(new Vector3(1, 1, Mathf.Abs(camera.transform.position.z - zDepth)));
            
            Rect fovBounds = new Rect(
                bottomLeft.x,
                bottomLeft.y,
                topRight.x - bottomLeft.x,
                topRight.y - bottomLeft.y
            );
            
            Rect allBounds = new Rect(
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

            return (fovBounds, allBounds, innerBufferBounds);
        }
        #endregion
        
        #if UNITY_EDITOR
        public void DrawGizmos()
        {
            if (camera != null)
            {
                var (fovBounds, mainBounds, bufferBounds) = CalculateCameraBounds(camera, zDepth, outerBuffer, innerBuffer);
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
                var colors = new[]
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