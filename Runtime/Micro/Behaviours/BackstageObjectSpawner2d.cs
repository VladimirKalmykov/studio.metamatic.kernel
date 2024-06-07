using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Metamatic;
using Metamatic.Actions;
using Metamatic.DevTools;
using Metamatic.MicroBehaviours;
using UnityEngine;
using UnityEngine.Pool;

namespace Micro.Behaviours
{
    public class BackstageObjectSpawner2d: MicroBehaviour
    {
        private readonly IObjectPool<GameObject> m_Pool;
        private int m_MaxObjectsCount;
        private bool m_PreWarm;
        private List<VirtualArea2d> m_Areas = new List<VirtualArea2d>();
        private bool m_Ready;
        private SequentialArrayHandler<VirtualArea2d> m_AreasHandler;
        private SequentialMapHandler<VirtualArea2d, CameraFOVTracker2d.AreaIntersection> m_AreaIntersectionHandler;
        private FOV2DService m_FOV2DService;
        private event System.Action OnFovManagerReady;
        private Dictionary<VirtualArea2d, List<Transform>> m_SpawnedObjectsMap = new Dictionary<VirtualArea2d, List<Transform>>();
        private List<DisposableAction> m_ObjectTracker = new List<DisposableAction>();
        private UniqueDice uniqueDice = new UniqueDice(2);
        private VirtualArea2d.GeneratePointsAlgorithm m_Algorithm;
        private Action<Transform> m_OnObjectSpawned;
        private float m_TotalAreasSize = 0;
        public BackstageObjectSpawner2d(
            IObjectPool<GameObject> pool,
            int maxObjectsCount,
            bool preWarm = false,
            VirtualArea2d.GeneratePointsAlgorithm algorithm = VirtualArea2d.GeneratePointsAlgorithm.Random,
            Action<Transform> onObjectSpawned = null
        )
        {
            m_Pool = pool;
            m_MaxObjectsCount = maxObjectsCount;
            m_PreWarm = preWarm;
            m_Algorithm = algorithm;
            m_OnObjectSpawned = onObjectSpawned;
            Run();
        }
        public void AddArea(VirtualArea2d area)
        {
            m_Areas.Add(area);
            if (m_FOV2DService != null)
            {
                m_AreasHandler.Handle(m_Areas);
                return;
            }
            OnFovManagerReady += () => m_AreasHandler.Handle(m_Areas);
        }
        public void RemoveArea(VirtualArea2d area)
        {
            m_Areas.Remove(area);
            if (m_FOV2DService != null)
            {
                m_AreasHandler.Handle(m_Areas);
                return;
            }
            OnFovManagerReady += () => m_AreasHandler.Handle(m_Areas);
        }
        protected override async UniTask Main(CancellationToken cancellationToken)
        {
            using var areaIntersectionHandler = m_AreaIntersectionHandler = new SequentialMapHandler<VirtualArea2d, CameraFOVTracker2d.AreaIntersection>(HandleAreaIntersection);
            using var areasArrayHandler = m_AreasHandler = new SequentialArrayHandler<VirtualArea2d>(HandleArea);
            using var fovManagerHandler = new SequentialHandler<FOV2DService>(HandleFOVManager);
            using var fovManagerSubscription = Dispatcher.global.OnRegistered<FOV2DService>(fovManagerHandler.Handle);
            
            await cancellationToken;
        }
        
        private async void HandleFOVManager(FOV2DService fovManager, CancellationToken cancellationToken)
        {
            if (fovManager == null)
            {
                return;
            }
            m_FOV2DService = fovManager;
            OnFovManagerReady?.Invoke();
            m_AreasHandler.Handle(m_Areas);
            /* Stack all areas */
            await cancellationToken;
            m_AreasHandler.Reset();
            m_FOV2DService = null;
            m_ObjectTracker.ForEach(t => t.Dispose());
        }

        private async void HandleArea(VirtualArea2d area, CancellationToken token)
        {
            m_TotalAreasSize += area.size;
            if (m_PreWarm)
            {
                try
                {
                    /* Track the area within the FOV bounds first frame */
                    var intersection =
                        await m_FOV2DService.TrackAreaOneShot(area, CameraFOVTracker2d.FovBoundsType.Fov, token);
                    /* Get currently spawned objects count */
                    if (!m_SpawnedObjectsMap.TryGetValue(intersection.area, out var spawnedObjects))
                    {
                        spawnedObjects = new List<Transform>();
                        m_SpawnedObjectsMap.Add(intersection.area, spawnedObjects);
                    }
                    /* Calculate the min distance between objects */
                    var minDistance = FindMinimumDistance(intersection.area.size, m_MaxObjectsCount);
                    var maxObjectsCountPerArea = (int)(m_MaxObjectsCount * (area.size / m_TotalAreasSize));
                    SpawnObjectsInIntersectedArea(intersection.area, intersection, spawnedObjects, (float)minDistance, maxObjectsCountPerArea);
                } catch (OperationCanceledException)
                {
                    return;
                }
            }

            using var tracker = m_FOV2DService.TrackArea(area, CameraFOVTracker2d.FovBoundsType.OuterBufferOnly,
                (intersected, intersection) =>
                {
                    m_AreaIntersectionHandler.Handle(area, intersection);
                });
            await token;
            m_TotalAreasSize -= area.size;
        }
        
        private async void HandleAreaIntersection(VirtualArea2d area, CameraFOVTracker2d.AreaIntersection intersection, CancellationToken cancellationToken)
        {
            if (!intersection.isIntersected)
            {
                area.overlapped = false;
                return;
            }
            area.overlapped = true;
            /* Calculate the min distance between objects */
            var minDistance = FindMinimumDistance(m_TotalAreasSize, m_MaxObjectsCount);
            /* Get currently spawned objects count */
            if (!m_SpawnedObjectsMap.TryGetValue(intersection.area, out var spawnedObjects))
            {
                spawnedObjects = new List<Transform>();
                m_SpawnedObjectsMap.Add(intersection.area, spawnedObjects);
            }
            /* Calculate objects count to spawn */
            var maxObjectsCountPerArea = (int)(m_MaxObjectsCount * (area.size / m_TotalAreasSize));
            using var loop = AsyncUtils.PlayerLoopAction(() =>
            {
                UILog.Add("spawnedObjects", "Count", spawnedObjects.Count);
                SpawnObjectsInIntersectedArea(intersection.area, intersection, spawnedObjects, (float)minDistance, maxObjectsCountPerArea);

            }, PlayerLoopTiming.FixedUpdate);
                
            await cancellationToken;
        }

        private void SpawnObjectsInIntersectedArea(VirtualArea2d area, CameraFOVTracker2d.AreaIntersection intersection, List<Transform> spawnedObjects, float minDistance, int maxObjectsCountPerArea)
        {
            if (spawnedObjects.Count >= maxObjectsCountPerArea)
            {
                return;
            }
            var spawnedPoints = spawnedObjects.ConvertAll(t => (Vector2)t.position);
            var intersectedRects = intersection.GetIntersectedRects();
            uniqueDice.SetSides(intersectedRects.Count);
            
            /* Debug */
            var debugStart = Time.realtimeSinceStartup;
            var debugCycles = 0;
            var pointsGenerationDuration = 0f;
            
            var successSpawned = true;
            while (successSpawned && spawnedObjects.Count < maxObjectsCountPerArea)
            {
                debugCycles++;
                successSpawned = false;
                while (uniqueDice.remainingSides > 0)
                {
                    var maxCounts = maxObjectsCountPerArea - spawnedObjects.Count;
                    var index = uniqueDice.Next();
                    var rect = intersectedRects[index];
                    var startGenerateRandomPoints = Time.realtimeSinceStartup;
                    var randomPoints = intersection.area.GetSpawnPoints(rect, minDistance, maxCounts, spawnedPoints, algorithm: m_Algorithm);
                    pointsGenerationDuration += (Time.realtimeSinceStartup - startGenerateRandomPoints) * 1000;
                    // var rp = intersection.area.GetRandomPosition(rect);
                    /* Ensure the minimum distance between objects */
                    // var maintainDistance = IfMaintainDistance(rp, spawnedPoints, (float)minDistance);
                    if (randomPoints.Length == 0)
                    {
                        successSpawned = false;
                        continue;
                    }

                    foreach (var rp in randomPoints)
                    {
                        var obj = m_Pool.Get();
                        obj.transform.position = rp;
                        spawnedObjects.Add(obj.transform);
                        spawnedPoints.Add(rp);
                        DisposableAction untrack = null;
                        obj.SetActive(true);
                        m_ObjectTracker.Add(TrackObject(intersection.area, obj));
                        successSpawned = true;
                        m_OnObjectSpawned?.Invoke(obj.transform);
                    }
                }
            }
            var performanceInMs = (Time.realtimeSinceStartup - debugStart) * 1000;
        }
        
        private DisposableAction TrackObject(VirtualArea2d area, GameObject obj)
        {
            bool untrackNow = false;
            DisposableAction untrack = default;
            untrack = m_FOV2DService.TrackObject(obj.transform, CameraFOVTracker2d.FovBoundsType.Full, (t, boundsType) =>
            {
                if (boundsType == CameraFOVTracker2d.FovBoundsType.None)
                {
                    untrackNow = true;
                    untrack.Dispose();
                    m_Pool.Release(obj);
                    var objectIndex = m_SpawnedObjectsMap[area].FindIndex(t => t == obj.transform);
                    m_SpawnedObjectsMap[area].RemoveAt(objectIndex);
                }
            });
            if (untrackNow)
            {
                untrack.Dispose();
            }
            
            return new DisposableAction(() =>
            {
                untrack.Dispose();
            });
        }
    
        public static double FindMinimumDistance(float areaSize, int count, double epsilon = 1e-4)
        {
            double sideLength = Mathf.Sqrt(areaSize);
        
            double low = 0;
            double high = sideLength;
            double mid = 0;

            while (high - low > epsilon)
            {
                mid = (low + high) / 2;
                if (CanPlaceObjects(sideLength, count, mid))
                {
                    low = mid;
                }
                else
                {
                    high = mid;
                }
            }

            return low;
        }

        private static bool CanPlaceObjects(double sideLength, int count, double minDist)
        {
            int objectsPerRow = (int)(sideLength / minDist);
            int maxObjects = objectsPerRow * objectsPerRow;

            return maxObjects >= count;
        }
    }
}