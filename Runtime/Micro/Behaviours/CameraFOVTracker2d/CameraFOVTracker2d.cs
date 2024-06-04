using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Metamatic.DevTools;

namespace Metamatic.MicroBehaviours
{
    using Unity.Jobs;
    using Unity.Collections;
    using Unity.Burst;
    using UnityEngine;
    
    /// <summary>
    /// MicroBehaviour that tracks spawn areas and emit events when they are inside the camera field of view
    /// </summary>
    public partial class CameraFOVTracker2d: MicroBehaviour
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
            { FovBoundsType.Full, new[] { 0 } },
            { FovBoundsType.Fov, new[] { 1 } },
            { FovBoundsType.InnerBuffer, new[] { 2 } },
            { FovBoundsType.OuterBufferOnly, new[] { 3, 4, 5, 6 } },
            { FovBoundsType.OuterLeft, new[] { 3 } },
            { FovBoundsType.OuterRight, new[] { 4 } },
            { FovBoundsType.OuterTop, new[] { 5 } },
            { FovBoundsType.OuterBottom, new[] { 6 } }
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
            /// <summary>
            /// Reference to the area itself
            /// </summary>
            public readonly VirtualArea2d area;
            /// <summary>
            /// Intersected rects mask (bitwise representation)
            /// </summary>
            public ulong intersectedRectsMask = 0;
            /// <summary>
            /// Intersected bounds types
            /// </summary>
            public FovBoundsType boundsTypes = 0;
            /// <summary>
            /// Is currently intersected
            /// </summary>
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

            public DisposableAction SubscribeRects(Action action)
            {
                OnIntersectedRectsChange += action;
                action.Invoke();
                return new DisposableAction(() => OnIntersectedRectsChange -= action);
            }
        }

        private class ObjectTracker: IDisposable
        {
            public readonly Transform transform;
            public FovBoundsType activatedBoundsTypesMask { get; private set; } = 0;
            private Dictionary<FovBoundsType, int> m_AcquiredBoundsTypeMap = new();
            private bool m_Pristine = true;
            public FovBoundsType acquiredBoundsTypesBinaryMask { get; private set; } = 0;
            private event Action<FovBoundsType> OnActivateChange;
            public int acquireCount { get; private set; }
            private bool m_Disposed = false;

            public ObjectTracker(Transform t)
            {
                this.transform = t;
            }
            ~ObjectTracker()
            {
                Dispose();
            }

            public bool ApplyBounds(FovBoundsType boundsType)
            {
                if (!m_Pristine && activatedBoundsTypesMask == boundsType)
                {
                    return false;
                }
                m_Pristine = false;

                activatedBoundsTypesMask = boundsType;
                OnActivateChange?.Invoke(activatedBoundsTypesMask);
                return true;
            }

            public DisposableAction Acquire(FovBoundsType boundsTypeMask, TrackObjectHandler onReactivate = null)
            {
                #if DEBUG
                if (m_Disposed)
                {
                    throw new ObjectDisposedException(nameof(ObjectTracker));
                }
                #endif
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

                BakeAcquiredBoundsTypes();

                Action<FovBoundsType> handler = null;
                if (onReactivate != null)
                {
                    var pristine = true;
                    var intersection = activatedBoundsTypesMask & boundsTypeMask;
                    if (!m_Pristine)
                    {
                        onReactivate.Invoke(transform, intersection);
                    }

                    handler = (activeBounds) =>
                    {
                        var nextIntersection = activeBounds & boundsTypeMask;
                        if (pristine || (nextIntersection != 0) != (intersection != 0))
                        {
                            pristine = false;
                            intersection = nextIntersection;
                            onReactivate.Invoke(transform, intersection);
                        }
                    };
                    
                    OnActivateChange += handler;
                }
                
                var isDisposed = false;
                return new DisposableAction(() =>
                {
                    if (isDisposed)
                    {
                        return;
                    }
                    isDisposed = true;
                    acquireCount--;
                    OnActivateChange -= handler;
                    foreach (var boundsType in boundsTypes)
                    {
                        if (m_AcquiredBoundsTypeMap.ContainsKey(boundsType))
                        {
                            m_AcquiredBoundsTypeMap[boundsType]--;
                        }
                    }

                    if (activatedBoundsTypesMask != 0)
                    {
                        /* Send deactivation event to release objects that  */
                        onReactivate?.Invoke(transform, 0);
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

            public void Dispose()
            {
                if (m_Disposed)
                {
                    return;
                }
                m_Disposed = true;

                GC.SuppressFinalize(this);
                OnActivateChange?.Invoke(0);
                OnActivateChange = null;
                m_AcquiredBoundsTypeMap.Clear();
            }
        }

        internal class AreaTracker : IDisposable
        {
            internal readonly CameraFOVTracker2d masterTracker;
            public readonly VirtualArea2d area;
            public FovBoundsType activatedBoundsTypesMask { get; private set; } = 0;
            private Dictionary<FovBoundsType, int> m_AcquiredBoundsTypeMap = new();
            public FovBoundsType acquiredBoundsTypesBinaryMask { get; private set; } = 0;
            private event Action<FovBoundsType, ulong, bool> OnActivateChange;
            public int acquireCount { get; private set; }
            private ulong m_IntersectedRectsMask = 0;
            private bool m_Disposed = false;
            private bool m_Pristine = true;

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
                
                if (!m_Pristine && activatedBoundsTypesMask == boundsType && m_IntersectedRectsMask == intersectedRectsMask)
                {
                    return false;
                }
                var wasPristine = m_Pristine;
                m_Pristine = false;

                m_IntersectedRectsMask = intersectedRectsMask;
                activatedBoundsTypesMask = boundsType;
                OnActivateChange?.Invoke(activatedBoundsTypesMask, m_IntersectedRectsMask, wasPristine);
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

                BakeAcquiredBoundsTypes();

                ulong interestedRectIndexesMask = 0;
                foreach (var boundsType in boundsTypes)
                {
                    var indexes = MapBoundsTypeToRectsIndex[boundsType];
                    foreach (var index in indexes)
                    {
                        interestedRectIndexesMask |= 1UL << index;
                    }
                }

                Action<FovBoundsType, ulong, bool> handler = null;
                AreaIntersection areaIntersection = null;
                if (onReactivate != null)
                {
                    areaIntersection = new AreaIntersection(this, area);
                    var intersection = activatedBoundsTypesMask & boundsTypeMask;
                    areaIntersection.intersectedRectsMask = m_IntersectedRectsMask & interestedRectIndexesMask;
                    areaIntersection.boundsTypes = intersection;
                    if (!m_Pristine)
                    {
                        onReactivate.Invoke(intersection != 0, areaIntersection);
                    }
                    
                    handler = (activeBounds, activeRectsMask, wasPristine) =>
                    {
                        var nextIntersection = activeBounds & boundsTypeMask;
                        var nextIntersectedRectsMask = activeRectsMask & interestedRectIndexesMask;
                        areaIntersection.SetIntersectedRectsMask(nextIntersectedRectsMask);
                        areaIntersection.boundsTypes = nextIntersection;
                        if (wasPristine || (nextIntersection != 0) != (intersection != 0))
                        {
                            intersection = nextIntersection;
                            onReactivate.Invoke(nextIntersection != 0, areaIntersection);
                        }
                    };
                    OnActivateChange += handler;
                }
                
                var isDisposed = false;
                return new DisposableAction(() =>
                {
                    if (isDisposed)
                    {
                        return;
                    }
                    isDisposed = true;
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
                    if (areaIntersection != null && onReactivate != null && areaIntersection.isIntersected)
                        /* Send deactivation event */
                    {
                        onReactivate.Invoke(false, new AreaIntersection(this, area));
                    }
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
                if (!m_Disposed)
                {
                    if (disposing)
                    {
                        ApplyBounds(0, 0);
                        OnActivateChange = null;
                        m_AcquiredBoundsTypeMap.Clear();
                    }

                    m_Disposed = true;
                }
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        public delegate void TrackObjectHandler(Transform t, FovBoundsType boundsType);
        public delegate void TrackAreaHandler(bool intersected, AreaIntersection intersection);

        private Camera m_CustomCamera;
        private float m_InnerBuffer;
        private float m_OuterBuffer;
        private bool m_TrackedAreasAreSwapped = false;
        private Dictionary<VirtualArea2d, AreaTracker> m_TrackedAreas;
        private Dictionary<VirtualArea2d, AreaTracker> m_TrackedAreasBuffer;
        private Dictionary<VirtualArea2d, AreaTracker> iterableTrackedAreas => m_TrackedAreasAreSwapped ? m_TrackedAreasBuffer : m_TrackedAreas;
        private Dictionary<VirtualArea2d, AreaTracker> mutableTrackedAreas => m_TrackedAreasAreSwapped ? m_TrackedAreas : m_TrackedAreasBuffer;

        private bool m_TrackedObjectsAreSwapped = false;
        private List<ObjectTracker> m_TrackedObjects;
        private List<ObjectTracker> m_TrackedObjectsBuffer;
        private List<ObjectTracker> iterableTrackedObjects => m_TrackedObjectsAreSwapped ? m_TrackedObjectsBuffer : m_TrackedObjects;
        private List<ObjectTracker> mutableTrackedObjects => m_TrackedObjectsAreSwapped ? m_TrackedObjects : m_TrackedObjectsBuffer;

        private float m_ZDepth;
        private bool m_Paused = false;
        
        /* Native arrays */
        internal NativeArray<Rect> allRects = new NativeArray<Rect>(7, Allocator.Persistent);
        internal NativeArray<FovBoundsType> mapBoundsTypesToRectIndexesBoundTypes;
        internal NativeArray<int> mapBoundsTypeToRectsStartIndex;
        internal NativeArray<int> mapBoundsTypeToRectsEndIndex;
    
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

        public event Action<VirtualArea2d, Rect> OnAreaActivated;
        public event Action<VirtualArea2d> OnAreaDeactivated;
        public event Action<Transform, FovBoundsType> OnObjectReactivate;
        public event Action OnPostTracking;
        public Camera camera
        {
            get => m_CustomCamera ??= Camera.main;
            set => m_CustomCamera = value;
        }

        public CameraFOVTracker2d(
            Camera camera = null,
            float innerBuffer = 0f,
            float outerBuffer = 1f,
            float zDepth = 0f,
            bool autoRun = true)
        {
            m_CustomCamera = camera;
            this.m_InnerBuffer = innerBuffer;
            this.m_OuterBuffer = outerBuffer;
            this.m_TrackedAreas = new Dictionary<VirtualArea2d, AreaTracker>();
            this.m_TrackedAreasBuffer = new Dictionary<VirtualArea2d, AreaTracker>();
            this.m_TrackedObjects = new List<ObjectTracker>();
            this.m_TrackedObjectsBuffer = new List<ObjectTracker>();
            this.m_ZDepth = zDepth;

            if (autoRun)
            {
                Run();
            }
        }

        public bool ContainsArea(VirtualArea2d area)
        {
            return mutableTrackedAreas.ContainsKey(area);
        }

        public DisposableAction TrackArea(VirtualArea2d area, FovBoundsType boundsType, TrackAreaHandler onReactivate = null)
        {
            if (!mutableTrackedAreas.TryGetValue(area, out var tracker))
            {
                tracker = new AreaTracker(this, area);
                mutableTrackedAreas.Add(area, tracker);
            }
            var release = (Action)tracker.Acquire(boundsType, onReactivate);

            return new DisposableAction(() =>
            {
                release();
                if (tracker.acquireCount == 0 && mutableTrackedAreas.ContainsKey(area))
                {
                    mutableTrackedAreas.Remove(area);
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
            foreach (var tracker in mutableTrackedObjects)
            {
                if (tracker.transform == t)
                    return true;
            }
            return false;
        }

        public DisposableAction TrackObject(Transform t, FovBoundsType boundsType = FovBoundsType.Full, TrackObjectHandler onActivate = null)
        {
            if (t == null) throw new ArgumentNullException(nameof(t));

            ObjectTracker tracker = mutableTrackedObjects.FirstOrDefault(tracker => tracker.transform == t);
            if (tracker == null)
            {
                tracker = new ObjectTracker(t);
            }

            var release = (Action)tracker.Acquire(boundsType, onActivate);
            
            mutableTrackedObjects.Add(tracker);

            return new DisposableAction(() =>
            {
                release();
                if (tracker.acquireCount == 0 && mutableTrackedObjects.Contains(tracker))
                {
                    tracker.Dispose();
                    mutableTrackedObjects.Remove(tracker);
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
        
        public void Pause()
        {
            m_Paused = true;
        }
        
        public void Resume()
        {
            m_Paused = false;
        }

        protected override async UniTask Main(CancellationToken cancellationToken)
        {
            /* Initialize native arrays */
            mapBoundsTypesToRectIndexesBoundTypes = new NativeArray<FovBoundsType>(MapBoundsTypeToRectsIndex.Count, Allocator.Persistent);
            mapBoundsTypeToRectsStartIndex = new NativeArray<int>(MapBoundsTypeToRectsIndex.Count, Allocator.Persistent);
            mapBoundsTypeToRectsEndIndex = new NativeArray<int>(MapBoundsTypeToRectsIndex.Count, Allocator.Persistent);
            var rectIndexesLength = MapBoundsTypeToRectsIndex.Values.Sum(arr => arr.Length);
            NativeArray<int> mapBoundsTypeToRectsRectIndexes = new NativeArray<int>(rectIndexesLength, Allocator.Persistent);
            var index = 0;
            for (var i = 0; i < MapBoundsTypeToRectsIndex.Count; i++)
            {
                var kvp = MapBoundsTypeToRectsIndex.ElementAt(i);
                mapBoundsTypesToRectIndexesBoundTypes[i] = kvp.Key;
                mapBoundsTypeToRectsStartIndex[i] = index;
                foreach (var subArr in kvp.Value)
                {
                    mapBoundsTypeToRectsRectIndexes[index] = subArr;
                    index++;
                }
                mapBoundsTypeToRectsEndIndex[i] = index - 1;
            }
            
            ulong intersectedRects = 0;
            FovBoundsType resultBoundsTypeMask = 0;
            var fovBoundsTypeArray = Enum.GetValues(typeof(FovBoundsType));
            
            float prevTime = Time.time;
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                if (m_Paused == true || m_CustomCamera == null)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update);
                    continue;
                }
                var debugStartTime = Time.realtimeSinceStartup;
                m_IsTracking = true;
                var (fovBounds, allBounds, innerBufferBounds) = CalculateCameraBounds(this.camera, m_ZDepth, outerBuffer, innerBuffer);

                allRects[0] = allBounds;
                allRects[1] = fovBounds;
                allRects[2] = innerBufferBounds;
                allRects[3] = new Rect(allBounds.xMin, allBounds.yMin, innerBufferBounds.xMin - allBounds.xMin, allBounds.height);
                allRects[4] = new Rect(innerBufferBounds.xMax, allBounds.yMin, allBounds.xMax - innerBufferBounds.xMax, allBounds.height);
                allRects[5] = new Rect(allBounds.xMin, innerBufferBounds.yMax, allBounds.width, allBounds.yMax - innerBufferBounds.yMax);
                allRects[6] = new Rect(allBounds.xMin, allBounds.yMin, allBounds.width, innerBufferBounds.yMin - allBounds.yMin);

                SwapTrackedAreas();
                SwapTrackedObjects();
                
                NativeArray<FovBoundsType> objectsAcquiredBoundsTypesBinaryMaskArray = new NativeArray<FovBoundsType>(iterableTrackedObjects.Count, Allocator.TempJob);
                NativeArray<Vector2> trackedObjectPositions = new NativeArray<Vector2>(iterableTrackedObjects.Count, Allocator.TempJob);
                NativeArray<FovBoundsType> objectsResults = new NativeArray<FovBoundsType>(iterableTrackedObjects.Count, Allocator.TempJob);
                
                /* Native arrays for areas */
                NativeArray<FovBoundsType> areaAcquiredBoundsTypesBinaryMaskArray = new NativeArray<FovBoundsType>(iterableTrackedAreas.Count, Allocator.TempJob);
                NativeArray<Rect> areaRects = new NativeArray<Rect>(iterableTrackedAreas.Count, Allocator.TempJob);
                NativeArray<ulong> intersectedRectsResult = new NativeArray<ulong>(iterableTrackedAreas.Count, Allocator.TempJob);
                NativeArray<FovBoundsType> areasResults = new NativeArray<FovBoundsType>(iterableTrackedAreas.Count, Allocator.TempJob);
                
                try
                {

                    for (var i = 0; i < iterableTrackedObjects.Count; i++)
                    {
                        var tracker = iterableTrackedObjects[i];
                        trackedObjectPositions[i] = tracker.transform.position;
                        objectsAcquiredBoundsTypesBinaryMaskArray[i] = tracker.acquiredBoundsTypesBinaryMask;
                    }

                    var trackObjectsJob = new TrackObjectsJob
                    {
                        allRects = allRects,
                        trackedObjectPositions = trackedObjectPositions,
                        acquiredBoundsTypesBinaryMaskArray = objectsAcquiredBoundsTypesBinaryMaskArray,
                        resultBoundsTypeMaskArray = objectsResults,
                        mapBoundsTypeToRectsRectIndexes = mapBoundsTypeToRectsRectIndexes,
                        mapBoundsTypeToRectsStartIndex = mapBoundsTypeToRectsStartIndex,
                        mapBoundsTypeToRectsEndIndex = mapBoundsTypeToRectsEndIndex,
                        mapBoundsTypesToRectIndexesBoundTypes = mapBoundsTypesToRectIndexesBoundTypes
                    };
                    var objectsJobHandle = trackObjectsJob.Schedule(iterableTrackedObjects.Count, 64);
                    
                    /* Tracking areas */
                    for (var i = 0; i < iterableTrackedAreas.Count; i++)
                    {
                        var (area, areaTrack) = iterableTrackedAreas.ElementAt(i);
                        areaRects[i] = area.rectRepresentation;
                        areaAcquiredBoundsTypesBinaryMaskArray[i] = areaTrack.acquiredBoundsTypesBinaryMask;
                    }
                    
                    var trackAreasJob = new TrackAreasJob
                    {
                        allRects = allRects,
                        areaRects = areaRects,
                        acquiredBoundsTypesBinaryMaskArray = areaAcquiredBoundsTypesBinaryMaskArray,
                        resultBoundsTypeMaskArray = areasResults,
                        mapBoundsTypeToRectsRectIndexes = mapBoundsTypeToRectsRectIndexes,
                        mapBoundsTypeToRectsStartIndex = mapBoundsTypeToRectsStartIndex,
                        mapBoundsTypeToRectsEndIndex = mapBoundsTypeToRectsEndIndex,
                        mapBoundsTypesToRectIndexesBoundTypes = mapBoundsTypesToRectIndexesBoundTypes,
                        intersectedRectsResult = intersectedRectsResult
                    };
                    var areasJobHandle = trackAreasJob.Schedule(iterableTrackedAreas.Count, 64, objectsJobHandle);
                    
                    /* Wait for jobs to complete */
                    await UniTask.WaitUntil(() => areasJobHandle.IsCompleted && objectsJobHandle.IsCompleted, PlayerLoopTiming.Update, cancellationToken);
                    areasJobHandle.Complete();
                    objectsJobHandle.Complete();
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    /* Parse objects job result */
                    for (var i = 0; i < iterableTrackedObjects.Count; i++)
                    {
                        var trackedObject = iterableTrackedObjects[i];
                        var hresultBoundsTypeMask = objectsResults[i];
                        if (trackedObject.ApplyBounds(hresultBoundsTypeMask))
                        {
                            OnObjectReactivate?.Invoke(trackedObject.transform, hresultBoundsTypeMask);
                        }
                    }
                    
                    /* Parse areas job result */
                    for (var i = 0; i < iterableTrackedAreas.Count; i++)
                    {
                        var (area, areaTrack) = iterableTrackedAreas.ElementAt(i);
                        var hresultBoundsTypeMask = areasResults[i];
                        var hintersectedRects = intersectedRectsResult[i];
                        if (areaTrack.ApplyBounds(hresultBoundsTypeMask, hintersectedRects))
                        {
                            if (hresultBoundsTypeMask != 0)
                            {
                                OnAreaActivated?.Invoke(area, area.rectRepresentation);
                            }
                            else
                            {
                                OnAreaDeactivated?.Invoke(area);
                            }
                        }
                    }

                    m_IsTracking = false;
                    OnPostTracking?.Invoke();
                    OnPostTracking = null;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                finally
                {
                    /* Dispose all the arrays */
                    objectsAcquiredBoundsTypesBinaryMaskArray.Dispose();
                    trackedObjectPositions.Dispose();
                    objectsResults.Dispose();
                    areaAcquiredBoundsTypesBinaryMaskArray.Dispose();
                    areaRects.Dispose();
                    intersectedRectsResult.Dispose();
                    areasResults.Dispose();
                }
                
                
                var performanceInMs = (Time.realtimeSinceStartup - debugStartTime) * 1000;
                UILog.Add("Performance", "CameraFOVTracker2d", $"{performanceInMs} ms");
                
                /* Await next fixed update */
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
                
                var frameDuration = Time.time - prevTime;
                prevTime = Time.time;
            }
            
            foreach (var (area, areaTrack) in m_TrackedAreas)
            {
                areaTrack.Dispose();
            }
            allRects.Dispose();
            mapBoundsTypesToRectIndexesBoundTypes.Dispose();
            mapBoundsTypeToRectsStartIndex.Dispose();
            mapBoundsTypeToRectsEndIndex.Dispose();
            mapBoundsTypeToRectsRectIndexes.Dispose();
        }

        private bool BoundsOverlapsArea(NativeArray<Rect> allRects, int[] rectIndexes, VirtualArea2d area, ref ulong intersectedRects)
        {
            for (int i = 0; i < rectIndexes.Length; i++)
            {
                var rectIndex = rectIndexes[i];
                if (area.Overlaps(allRects[rectIndex]))
                {
                    intersectedRects |= 1UL << rectIndex;
                }
            }
            return intersectedRects > 0;
        }

        private float m_CameraFieldOfView = 0;
        private float m_CameraAspect = 0;
        private Vector3 m_CameraPosition = Vector3.zero;
        private (Rect fovBounds, Rect mainBounds, Rect innerBufferBounds) m_CameraBounds;
        private (Rect fovBounds, Rect mainBounds, Rect innerBufferBounds) CalculateCameraBounds(Camera camera, float zDepth, float outerBuffer, float innerBuffer)
        {
            if (m_CameraFieldOfView == camera.fieldOfView && m_CameraAspect == camera.aspect && m_CameraPosition == camera.transform.position)
            {
                return m_CameraBounds;
            }
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

            m_CameraBounds = (fovBounds, allBounds, innerBufferBounds);
            return m_CameraBounds;
        }

        private void SwapTrackedAreas()
        {
            (m_TrackedAreas, m_TrackedAreasBuffer) = (m_TrackedAreasBuffer, m_TrackedAreas);
            mutableTrackedAreas.Clear();
            foreach (var kvp in iterableTrackedAreas)
            {
                mutableTrackedAreas.Add(kvp.Key, kvp.Value);
            }
            m_TrackedAreasAreSwapped = !m_TrackedAreasAreSwapped;
        }

        private void SwapTrackedObjects()
        {
            (m_TrackedObjects, m_TrackedObjectsBuffer) = (m_TrackedObjectsBuffer, m_TrackedObjects);
            mutableTrackedObjects.Clear();
            foreach (var tracker in iterableTrackedObjects)
            {
                mutableTrackedObjects.Add(tracker);
            }
            m_TrackedObjectsAreSwapped = !m_TrackedObjectsAreSwapped;
        }

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

                var colors = new[]
                {
                    new Color(0, 1, 0, 0.5f),
                    new Color(0, 0, 1, 0.5f),
                    new Color(1, 0, 0, 0.5f),
                    new Color(1, 0, 1, 0.5f),
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
