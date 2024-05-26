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
        public enum FovBoundsType
        {
            None = 0,
            /// <summary>
            /// All bounds including FOV, inner buffer and outer buffer
            /// </summary>
            Full = 1,
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
        
        private static readonly Dictionary<FovBoundsType, int[]> MapBoundsTypeToRectsIndex = new()
        {
            {FovBoundsType.Full, new[]{0}},
            {FovBoundsType.Fov, new[]{1}},
            {FovBoundsType.InnerBuffer, new[]{2}},
            {FovBoundsType.OuterBufferOnly, new[]{3, 4, 5, 6}},
            {FovBoundsType.OuterLeft, new[]{3}},
            {FovBoundsType.OuterRight, new[]{4}},
            {FovBoundsType.OuterTop, new[]{5}},
            {FovBoundsType.OuterBottom, new[]{6}}
        };
        
        private static FovBoundsType[] DecomposeBounds(FovBoundsType bounds)
        {
            List<FovBoundsType> result = new List<FovBoundsType>();

            foreach (FovBoundsType value in Enum.GetValues(typeof(FovBoundsType)))
            {
                if (value != FovBoundsType.None && (bounds & value) == value)
                {
                    result.Add(value);
                }
            }

            return result.ToArray();
        }
        
        public class AreaIntersection
        {
            private readonly AreaTracker m_AreaTracker;
            public readonly VirtualArea2d area;
            public ulong intersectedRectsMask = 0;
            public FovBoundsType boundsTypes = 0;
            public bool isIntersected => intersectedRectsMask > 0;
            private readonly List<Rect> m_IntersectedRects = new List<Rect>(7);
            public event Action OnIntersectedRectsChange;
            internal AreaIntersection(AreaTracker areaTracker, VirtualArea2d area)
            {
                this.m_AreaTracker = areaTracker;
                this.area = area;
            }
            
            public void SetIntersectedRectsMask(ulong mask)
            {
                if (intersectedRectsMask == mask)
                {
                    return;
                }
                intersectedRectsMask = mask;
                OnIntersectedRectsChange?.Invoke();
            }
            
            public List<Rect> GetIntersectedRects()
            {
                m_IntersectedRects.Clear();
                for (int i = 0; i < 7; i++)
                {
                    if ((intersectedRectsMask & (1UL << i)) != 0)
                    {
                        m_IntersectedRects.Add(m_AreaTracker.masterTracker.allRects[i]);
                    }
                }

                return m_IntersectedRects;
            }
        }
        
        private class ObjectTracker
        {
            public readonly Transform transform;
            public FovBoundsType activatedBoundsTypesMask { get; private set; } = 0;
            private Dictionary<FovBoundsType, int> m_AcquiredBoundsTypeMap = new();
            public FovBoundsType acquiredBoundsTypesBinaryMask { get; private set; }= 0;
            private event Action<FovBoundsType> OnActivateChange;
            public int acquireCount { get; private set; }
            public ObjectTracker(Transform t)
            {
                this.transform = t;
            }

            public bool ApplyBounds(FovBoundsType boundsType)
            {
                if (activatedBoundsTypesMask == boundsType)
                {
                    return false;
                }
                
                Debug.Log($"ApplyBounds: {boundsType} vs {activatedBoundsTypesMask}");
                activatedBoundsTypesMask = boundsType;
                OnActivateChange?.Invoke(activatedBoundsTypesMask);
                return true;
            }
            
            public DisposableAction Acquire(FovBoundsType boundsTypeMask, TrackObjectHandler onReactivate = null)
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
                Action<FovBoundsType> handler = null;
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
        internal class AreaTracker : IDisposable
        {
            internal readonly CameraFOVTracker2d masterTracker;
            public readonly VirtualArea2d area;
            public FovBoundsType activatedBoundsTypesMask { get; private set; } = 0;
            private Dictionary<FovBoundsType, int> m_AcquiredBoundsTypeMap = new();
            public FovBoundsType acquiredBoundsTypesBinaryMask { get; private set; } = 0;
            private event Action<FovBoundsType, ulong> OnActivateChange;
            public int acquireCount { get; private set; }
            private ulong m_IntersectedRectsMask = 0;
            private bool disposed = false;

            public AreaTracker(CameraFOVTracker2d masterTracker, VirtualArea2d area)
            {
                this.masterTracker = masterTracker;
                this.area = area;
            }
            
            ~AreaTracker()
            {
                Dispose(false);
            }

            public bool ApplyBounds(FovBoundsType boundsType, ulong intersectedRectsMask)
            {
                if (activatedBoundsTypesMask == boundsType && m_IntersectedRectsMask == intersectedRectsMask)
                {
                    /* No changes */
                    return false;
                }

                m_IntersectedRectsMask = intersectedRectsMask;
                activatedBoundsTypesMask = boundsType;
                OnActivateChange?.Invoke(activatedBoundsTypesMask, m_IntersectedRectsMask);
                return true;
            }

            public DisposableAction Acquire(FovBoundsType boundsTypeMask, TrackAreaHandler onReactivate = null)
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

                /* Compile boundsTypesMark to interested rect indexes mask */
                ulong interestedRectIndexesMask = 0;
                var interestedRectsCount = 0;
                foreach (var boundsType in boundsTypes)
                {
                    var indexes = MapBoundsTypeToRectsIndex[boundsType];
                    foreach (var index in indexes)
                    {
                        interestedRectIndexesMask |= 1UL << index;
                        interestedRectsCount++;
                    }
                }

                /* If callback is provided */
                Action<FovBoundsType, ulong> handler = null;
                if (onReactivate != null)
                {
                    var areaIntersection = new AreaIntersection(this, area);
                    /* Compare to current activated mask and trigger reactivation */
                    var intersection = activatedBoundsTypesMask & boundsTypeMask;
                    areaIntersection.intersectedRectsMask = m_IntersectedRectsMask & interestedRectIndexesMask;
                    areaIntersection.boundsTypes = intersection;
                    onReactivate.Invoke(intersection != 0, areaIntersection);
                    handler = (activeBounds, activeRectsMask) =>
                    {
                        var nextIntersection = activeBounds & boundsTypeMask;
                        var nextIntersectedRectsMask = activeRectsMask & interestedRectIndexesMask;
                        var rectsChanged = nextIntersectedRectsMask != areaIntersection.intersectedRectsMask;
                        areaIntersection.SetIntersectedRectsMask(nextIntersectedRectsMask);
                        areaIntersection.boundsTypes = nextIntersection;
                        /* Compare if intersection is changed */
                        if ((nextIntersection != 0) != (intersection != 0))
                        {
                            intersection = nextIntersection;
                            onReactivate.Invoke(nextIntersection != 0, areaIntersection);
                        }
                        /* Get from activeRectsMask only those that are intersected */
                        var intersectedRectsMask = activeRectsMask & interestedRectIndexesMask;
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

            protected virtual void Dispose(bool disposing)
            {
                if (!disposed)
                {
                    if (disposing)
                    {
                        // Освобождение управляемых ресурсов
                        ApplyBounds(0, 0);
                        OnActivateChange = null;
                        m_AcquiredBoundsTypeMap.Clear();
                    }

                    // Освобождение неуправляемых ресурсов (если есть)
                    disposed = true;
                }
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this); // Подавляем финализацию, так как ресурсы уже освобождены
            }
        }

        
        public delegate void TrackObjectHandler(Transform t, FovBoundsType boundsType);
        public delegate void TrackAreaHandler(bool intersected, AreaIntersection intersection);
        
        private Camera m_CustomCamera;
        private float m_InnerBuffer;
        private float m_OuterBuffer;
        private Dictionary<VirtualArea2d, AreaTracker> m_TrackedAreas;
        private List<ObjectTracker> m_TrackedObjects;
        private float m_ZDepth;
        internal readonly Rect[] allRects = new Rect[7];
        public IReadOnlyList<Rect> rects => Array.AsReadOnly(allRects);

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
        public event Action<Transform, FovBoundsType> OnObjectReactivate;
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
        public DisposableAction SubscribeObjectReactivate(Action<Transform, FovBoundsType> action)
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
        /// <param name="onReactivate"></param>
        /// <returns></returns>
        public DisposableAction TrackArea(VirtualArea2d area, FovBoundsType boundsType, TrackAreaHandler onReactivate = null)
        {
            var cancelled = false;
            
            if (!m_TrackedAreas.TryGetValue(area, out var tracker))
            {
                tracker = new AreaTracker(this, area);
                SafeExecute(() =>
                {
                    if (cancelled) return;
                    m_TrackedAreas.Add(area, tracker);
                });
            }
            var release = (Action)tracker.Acquire(boundsType, onReactivate);

            return new DisposableAction(() =>
            {
                cancelled = true;
                release();
                if (tracker.acquireCount == 0 && m_TrackedAreas.ContainsKey(area))
                {
                    m_TrackedAreas.Remove(area);
                }
            });
        }
        
        public DisposableAction TrackAreas(IEnumerable<VirtualArea2d> areas, FovBoundsType boundsType, TrackAreaHandler onAreaActivated = null)
        {
            var disposables = areas.Select(area => TrackArea(area, boundsType, onAreaActivated)).ToList();
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
        
        public DisposableAction TrackObject(Transform t, FovBoundsType boundsType = FovBoundsType.Full, TrackObjectHandler onActivate = null)
        {
            var cancelled = false;
            /* Create object tracker if not exists */
            ObjectTracker tracker = m_TrackedObjects.FirstOrDefault(tracker => tracker.transform == t);
            if (tracker == null)
            {
                tracker = new ObjectTracker(t);
                SafeExecute(() =>
                {
                    
                    if (cancelled) return;
                    m_TrackedObjects.Add(tracker);
                });
            }
                
            var release = (Action)tracker.Acquire(boundsType, onActivate);

            return new DisposableAction(() =>
            {
                cancelled = true;
                release();
                if (tracker.acquireCount == 0 && m_TrackedObjects.Contains(tracker))
                {
                    m_TrackedObjects.Remove(tracker);
                }
            });
        }
        
        public DisposableAction TrackObjects(IEnumerable<Transform> transforms, FovBoundsType boundsType = FovBoundsType.Full, TrackObjectHandler onActivate = null)
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
            ulong intersectedRects = 0;
            FovBoundsType resultBoundsTypeMask = 0;
            var fovBoundsTypeArray = Enum.GetValues(typeof(FovBoundsType));
            using var lifecycle = AsyncUtils.PlayerLoopAction(() =>
            {
                m_IsTracking = true;
                var (fovBounds, allBounds, innerBufferBounds) = CalculateCameraBounds(this.camera, m_ZDepth, outerBuffer, innerBuffer);
                
                /* All */
                allRects[0] = allBounds;
                /* FOV */
                allRects[1] = fovBounds;
                /* Inner buffer */
                allRects[2] = innerBufferBounds;
                /* Left */
                allRects[3] = new Rect(allBounds.xMin, allBounds.yMin, innerBufferBounds.xMin - allBounds.xMin,
                    allBounds.height);
                /* Right */
                allRects[4] = new Rect(innerBufferBounds.xMax, allBounds.yMin, allBounds.xMax - innerBufferBounds.xMax,
                    allBounds.height);
                /* Top */
                allRects[5] = new Rect(allBounds.xMin, innerBufferBounds.yMax, allBounds.width,
                    allBounds.yMax - innerBufferBounds.yMax);
                /* Bottom */
                allRects[6] = new Rect(allBounds.xMin, allBounds.yMin, allBounds.width,
                    innerBufferBounds.yMin - allBounds.yMin);
                    
                if (m_TrackedAreas != null) {
                    /* Track areas */
                    foreach (var (area, areaTrack) in m_TrackedAreas)
                    {
                        /* Clear intersected rects */
                        intersectedRects = 0;
                        resultBoundsTypeMask = 0;
                        /* t.acquiredBoundsTypesBinaryMask is a binary mask of bounds types that are acquired. Step every possible boundsType and calculate overlapping */
                        var mask = areaTrack.acquiredBoundsTypesBinaryMask;
                        foreach (FovBoundsType boundsType in fovBoundsTypeArray)
                        {
                            if ((mask & boundsType) != 0)
                            {
                                if (BoundsOverlapsArea(allRects, MapBoundsTypeToRectsIndex[boundsType], area, ref intersectedRects))
                                {
                                    resultBoundsTypeMask |= boundsType;
                                }
                                else
                                {
                                    resultBoundsTypeMask &= ~boundsType;
                                }
                            }
                        }

                        if (areaTrack.ApplyBounds(resultBoundsTypeMask, intersectedRects))
                        {
                            // Send intersection event
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
                        FovBoundsType resultBoundsTypeMask = 0;
                        foreach (FovBoundsType boundsType in Enum.GetValues(typeof(FovBoundsType)))
                        {
                            if ((mask & boundsType) != 0)
                            {
                                if (BoundsContainsPoint(allRects, MapBoundsTypeToRectsIndex[boundsType], t))
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
            /* Deactivate all area trackers */
            foreach (var (area, areaTrack) in m_TrackedAreas)
            {
                areaTrack.Dispose();
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
        
        private bool BoundsOverlapsArea(Rect[] allRects, int[] boundsTypes, VirtualArea2d area, ref ulong intersectedRects)
        {
            for (int i = 0; i < boundsTypes.Length; i++)
            {
                var rectIndex = boundsTypes[i];
                if (area.Overlaps(allRects[rectIndex]))
                {
                    intersectedRects |= 1UL << rectIndex;
                }
            }
            return intersectedRects > 0;
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
                for (var i = 3; i < allRects.Length; i++)
                {
                    Gizmos.color = colors[i - 3];
                    var sideRect = allRects[i];
                    Gizmos.DrawCube(new Vector3(sideRect.center.x, sideRect.center.y, zDepth), new Vector3(sideRect.width, sideRect.height, 0));
                }
            }
        }
        #endif
    }
}