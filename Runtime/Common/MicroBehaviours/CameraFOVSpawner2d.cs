using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Metamatic.MicroBehaviours
{
    public class CameraFOVSpawner2d: MicroBehaviour
    {
        class AreaSpawnPoint
        {
            public readonly Vector2 position;
            public Transform spawned;
            public AreaSpawnPoint(Vector2 position)
            {
                this.position = position;
                spawned = null;
            }
        }
        
        private CameraFOVTracker2d m_CameraFOVTracker;
        private Func<GameObject> onCreateObject;
        private Action<GameObject> onReleaseObject;
        private float m_MinCount = 3;
        private float m_MaxCount = 10;
        
        
        public CameraFOVTracker2d cameraFOVTracker => m_CameraFOVTracker;
        public float minCount => m_MinCount;
        public float maxCount => m_MaxCount;
        
        private List<Transform> m_SpawnedObjects = new List<Transform>();
        private Dictionary<VirtualArea2d, List<Transform>> m_AreaSpawnedObjectMap = new Dictionary<VirtualArea2d, List<Transform>>();
        private Dictionary<VirtualArea2d, AreaSpawnPoint[]> m_AreaPrebakedSpawnPointsMap = new Dictionary<VirtualArea2d, AreaSpawnPoint[]>();
        private Dictionary<Transform, VirtualArea2d> m_ObjectToAreaMap = new Dictionary<Transform, VirtualArea2d>();
        
        
        public CameraFOVSpawner2d(
            CameraFOVTracker2d cameraFOVTracker2d,
            Func<GameObject> onCreateObject,
            Action<GameObject> onReleaseObject = null,
            float minCount = 3,
            float maxCount = 10
        ) {
            m_CameraFOVTracker = cameraFOVTracker2d;
            Run();
        }
        protected override async UniTask Main(CancellationToken cancellationToken)
        {
            using var pool = new GameObjectPool(onCreateObject, false, 10);
            using var areaOverlappedSubscription = cameraFOVTracker.SubscribeOnAreaOverlapped(HandleAreaOverlapped);
            using var objectNotOverlappedSubscription = cameraFOVTracker.SubscribeObjectReactivate(HandleTransformNotOverlapped);
            await cancellationToken;
        }
        
        private void HandleTransformNotOverlapped(Transform t)
        {
            cameraFOVTracker.UntrackObject(t);
            m_ObjectToAreaMap.TryGetValue(t, out var area);
            if (area != null)
            {
                m_ObjectToAreaMap.Remove(t);
                m_AreaSpawnedObjectMap[area].Remove(t);
                m_AreaPrebakedSpawnPointsMap[area].First(x => x.spawned == t).spawned = null;
                onReleaseObject?.Invoke(t.gameObject);
            }
        }

        private void HandleAreaOverlapped(VirtualArea2d area, Rect rect)
        {
            /* Check objects count */
            var maxAreaCount = m_AreaPrebakedSpawnPointsMap[area].Length;
            var currentAreaCount = m_AreaSpawnedObjectMap[area].Count;
            if (currentAreaCount < maxAreaCount)
            {
                /* Find free area spawn point */
                AreaSpawnPoint freeAreaSpawnPoint = null;
                foreach (var areaSpawnPoint in m_AreaPrebakedSpawnPointsMap[area])
                {
                    if (areaSpawnPoint.spawned == null && rect.Contains(areaSpawnPoint.position))
                    {
                        freeAreaSpawnPoint = areaSpawnPoint;
                        /* Get random position inside the rect */
                        var spawnPosition = freeAreaSpawnPoint.position;
                        /* Get random object */
                        var go = onCreateObject();
                        go.transform.position = spawnPosition;
                        m_SpawnedObjects.Add(go.transform);
                        m_AreaSpawnedObjectMap[area].Add(go.transform);
                        m_ObjectToAreaMap.Add(go.transform, area);
                        freeAreaSpawnPoint.spawned = go.transform;
                        go.SetActive(true);
                        cameraFOVTracker.TrackObject(go.transform);
                        break;
                    }
                }
            }
        }
        
        public DisposableAction TrackArea(VirtualArea2d area)
        {
            m_AreaSpawnedObjectMap.Add(area, new List<Transform>());
            /* Generate spawn points on virtual area */
            var size = area.areaSize;
            var count = Mathf.FloorToInt(Random.Range(minCount * size, maxCount * size));
            var areaSpawnPoints = new AreaSpawnPoint[count];
            for (var i = 0; i < count; i++)
            {
                areaSpawnPoints[i] = new AreaSpawnPoint(area.GetRandomPosition());
            }
            m_AreaPrebakedSpawnPointsMap.Add(area, areaSpawnPoints);
            cameraFOVTracker.TrackArea(area);
            
            return new DisposableAction(() => UntrackArea(area));
        }
        
        public void UntrackArea(VirtualArea2d area)
        {
            if (!cameraFOVTracker.ContainsArea(area)) return;
            m_AreaSpawnedObjectMap.Remove(area);
            m_AreaPrebakedSpawnPointsMap.Remove(area);
            cameraFOVTracker.UntrackArea(area);
        }
        
        public DisposableAction TrackAreas(IEnumerable<VirtualArea2d> areas)
        {
            var disposables = areas.Select(TrackArea).ToList();
            return new DisposableAction(() =>
            {
                foreach (var disposable in disposables)
                {
                    disposable.Dispose();
                }
            }); 
        }

        public void DrawGizmos()
        {
            m_CameraFOVTracker.DrawGizmos();
        }
    }
}