using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Metamatic
{
    public class VirtualAreaRect2d: VirtualArea2d
    {
        [BurstCompile]
        public struct GeneratePointsJob : IJob
        {
            [ReadOnly] public NativeArray<Rect> rectValues;
            [ReadOnly] public NativeArray<float> floatValues;
            [ReadOnly] public NativeArray<Vector2> spawnedPoints;
            [ReadOnly] public NativeArray<GeneratePointsAlgorithm> algorithm;
            public NativeArray<uint> intValues;
            public NativeArray<Vector2> outputPoints;
            public void Execute()
            {
                var otherRect = rectValues[0];
                var worldRect = rectValues[1];
                var minDistance = floatValues[0];
                var maxCount = intValues[0];
                var seed = intValues[1];
                var algorithm = this.algorithm[0];
                if (!otherRect.Overlaps(worldRect, true))
                {
                    intValues[3] = 1; // Rectangles do not overlap.
                }

                Rect intersectRect = Rect.MinMaxRect(
                    Mathf.Max(otherRect.xMin, worldRect.xMin),
                    Mathf.Max(otherRect.yMin, worldRect.yMin),
                    Mathf.Min(otherRect.xMax, worldRect.xMax),
                    Mathf.Min(otherRect.yMax, worldRect.yMax)
                );
                
                float startX = intersectRect.xMin + minDistance / 2;
                float startY = intersectRect.yMin + minDistance / 2;
                float endX = intersectRect.xMax - minDistance / 2;
                float endY = intersectRect.yMax - minDistance / 2;
                
                var index = 0;
                var random = new Unity.Mathematics.Random(seed);
                
                /* Choose algorithm */
                switch (algorithm)
                {
                    case GeneratePointsAlgorithm.Random:
                        for (int i = 0; i < maxCount; i++)
                        {
                            if (index >= maxCount - 1)
                            {
                                break;
                            }
                            /* Get random x and y, but without using Random.Range, because it is not thread safe */
                            var randomX = startX + ((endX - startX) * (float)random.NextDouble());
                            var randomY = startY + ((endY - startY) * (float)random.NextDouble());
                            if (!IfMaintainDistance(new Vector2(randomX, randomY), spawnedPoints, outputPoints, index, minDistance))
                            {
                                continue;
                            }
                            outputPoints[index] = new Vector2(randomX, randomY);
                            index++;
                        }
                        intValues[2] = (uint)index;
                        return;
                    case GeneratePointsAlgorithm.Strict:
                        for (float x = startX; x <= endX; x += minDistance)
                        {
                            for (float y = startY; y <= endY; y += minDistance)
                            {
                                if (index >= maxCount - 1)
                                {
                                    break;
                                }
                                /* Ensure the minimum distance between objects */
                                if (!IfMaintainDistance(new Vector2(x, y), spawnedPoints, outputPoints, index, minDistance))
                                {
                                    continue;
                                }

                                outputPoints[index] = new Vector2(x, y);
                                index++;
                            }
                        }
                        intValues[2] = (uint)index;
                        return;
                }
                intValues[2] = (uint)index;
            }
            
            private static bool IfMaintainDistance(Vector2 t, NativeArray<Vector2> points, NativeArray<Vector2> outputPoints, int outputPointsLength, float minDistance)
            {
                foreach (var point in points)
                {
                    if (Vector2.Distance(t, point) < minDistance)
                    {
                        return false;
                    }
                }
                for (int i = 0; i < outputPointsLength; i++)
                {
                    if (Vector2.Distance(t, outputPoints[i]) < minDistance)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        [FormerlySerializedAs("rect")] [SerializeField] private Rect m_Rect;
        public override float size
        {
            get
            {
                var worldRect = rect;
                return worldRect.width * worldRect.height;
            }
        }

        public override Rect rectRepresentation => rect;

        public Rect rect {
            get
            {
                Vector2 localMin = m_Rect.min;
                Vector2 localMax = m_Rect.max;

                Vector2 worldMin = transform.TransformPoint(localMin);
                Vector2 worldMax = transform.TransformPoint(localMax);

                Rect worldRect = new Rect(worldMin, worldMax - worldMin);
                return worldRect;
            }
        }
        public override Vector2 GetRandomPosition(bool local = false)
        {
            /* Get random position on rect */
            var worldRect = local ? m_Rect : this.rect;
            return new Vector3(UnityEngine.Random.Range(worldRect.xMin, worldRect.xMax), UnityEngine.Random.Range(worldRect.yMin, worldRect.yMax));
        }

        public override Vector2 GetRandomPosition(Rect otherRect)
        {
            var worldRect = rect;
            if (!otherRect.Overlaps(worldRect))
            {
                throw new System.ArgumentException("Rectangles do not overlap.");
            }

            Rect intersectRect = Rect.MinMaxRect(
                Mathf.Max(otherRect.xMin, worldRect.xMin),
                Mathf.Max(otherRect.yMin, worldRect.yMin),
                Mathf.Min(otherRect.xMax, worldRect.xMax),
                Mathf.Min(otherRect.yMax, worldRect.yMax)
            );

            float randomX = Random.Range(intersectRect.xMin, intersectRect.xMax);
            float randomY = Random.Range(intersectRect.yMin, intersectRect.yMax);

            return new Vector2(randomX, randomY);
        }
        
        public override Vector2[] GetSpawnPoints(Rect otherRect, float minDistance, int maxCount, List<Vector2> spawnedObjects,  GeneratePointsAlgorithm algorithm = GeneratePointsAlgorithm.Random)
        {
            NativeArray<Vector2> outputPoints = new NativeArray<Vector2>(maxCount, Allocator.TempJob);
            NativeArray<Rect> rectValues = new NativeArray<Rect>(2, Allocator.TempJob);
            rectValues[0] = otherRect;
            rectValues[1] = rect;
            NativeArray<uint> intValues = new NativeArray<uint>(4, Allocator.TempJob);
            intValues[0] = (uint)maxCount;
            intValues[1] = (uint)UnityEngine.Random.Range(1, 10000);
            NativeArray<float> floatValues = new NativeArray<float>(1, Allocator.TempJob);
            floatValues[0] = minDistance;
            NativeArray<Vector2> spawnedPoints = new NativeArray<Vector2>(spawnedObjects.ToArray(), Allocator.TempJob);
            NativeArray<GeneratePointsAlgorithm> algorithms = new NativeArray<GeneratePointsAlgorithm>(1, Allocator.TempJob);
            algorithms[0] = algorithm;
            var job = new GeneratePointsJob
            {
                rectValues = rectValues,
                intValues = intValues,
                floatValues = floatValues,
                spawnedPoints = spawnedPoints,
                outputPoints = outputPoints,
                algorithm = algorithms
            };
            var jobHandle = job.Schedule();
            jobHandle.Complete();
            var error = intValues[3];

            Vector2[] result = null;
            if (error != 0)
            {
                if (error == 1)
                {
                    Debug.LogWarning("Rectangles do not overlap.");
                }
                else
                {
                    Debug.LogWarning("Unknown error.");
                }

                result = Array.Empty<Vector2>();
            }
            else
            {
                var totalCount = (int)intValues[2];
                result = outputPoints.AsSpan().Slice(0, totalCount).ToArray();
            }
            
            outputPoints.Dispose();
            rectValues.Dispose();
            intValues.Dispose();
            floatValues.Dispose();
            spawnedPoints.Dispose();
            algorithms.Dispose();
            return result;
        }
        /* TODO: Move it to some static helper class */
        public static float FindMinimumDistance(float areaSize, int count, double epsilon = 1e-4)
        {
            float sideLength = Mathf.Sqrt(areaSize);
        
            float low = 0;
            float high = sideLength;
            float mid = 0;

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

        public override bool Overlaps(Rect rect)
        {
            return this.rect.Overlaps(rect, true);
        }

        public override float OverlapsAreaSize(Rect otherRect)
        {
            var worldRect = rect;
            if (!otherRect.Overlaps(worldRect))
            {
                return 0f;
            }

            Rect intersectRect = Rect.MinMaxRect(
                Mathf.Max(otherRect.xMin, worldRect.xMin),
                Mathf.Max(otherRect.yMin, worldRect.yMin),
                Mathf.Min(otherRect.xMax, worldRect.xMax),
                Mathf.Min(otherRect.yMax, worldRect.yMax)
            );

            return intersectRect.width * intersectRect.height;
        }
        
        public override Vector2[] GeneratePoints(int count, GeneratePointsAlgorithm algorithm = GeneratePointsAlgorithm.Random)
        {
            switch (algorithm)
            {
                case GeneratePointsAlgorithm.Random:
                    var points = new List<Vector2>();
                    for (int i = 0; i < count; i++)
                    {
                        points.Add(GetRandomPosition());
                    }

                    return points.ToArray();
                case GeneratePointsAlgorithm.Strict:
                    return GeneratePointsStrict(count);
            }

            throw new Exception("Not supported algorithm");
        }
        
        private Vector2[] GeneratePointsStrict(int count)
        {
            var worldRect = rect;
            var maxCount = count;
            
            var minDistance = FindMinimumDistance(size, count);
            var result = new List<Vector2>();
            float startX = worldRect.xMin + minDistance / 2;
            float startY = worldRect.yMin + minDistance / 2;
            float endX = worldRect.xMax - minDistance / 2;
            float endY = worldRect.yMax - minDistance / 2;

            var index = 0;
            for (float x = startX; x <= endX; x += minDistance)
            {
                if (index >= maxCount - 1)
                {
                    break;
                }
                for (float y = startY; y <= endY; y += minDistance)
                {
                    result.Add(new Vector2(x, y));
                    index++;
                }
            }

            return result.ToArray();
        }

#if UNITY_EDITOR
        public override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            Handles.color = overlapped ? Color.red : debugColor;
            var worldRect = this.rect;
            Handles.DrawDottedLines(new []
            {
                (Vector3)(worldRect.center + new Vector2(worldRect.width / 2, -worldRect.height / 2f)),
                (Vector3)(worldRect.center + new Vector2(worldRect.width / 2, worldRect.height / 2f)),
                (Vector3)(worldRect.center + new Vector2(worldRect.width / 2, worldRect.height / 2f)),
                (Vector3)(worldRect.center + new Vector2(-worldRect.width / 2, worldRect.height / 2f)),
                (Vector3)(worldRect.center + new Vector2(-worldRect.width / 2, worldRect.height / 2f)),
                (Vector3)(worldRect.center + new Vector2(-worldRect.width / 2, -worldRect.height / 2f)),
                (Vector3)(worldRect.center + new Vector2(-worldRect.width / 2, -worldRect.height / 2f)),
                (Vector3)(worldRect.center + new Vector2(worldRect.width / 2, -worldRect.height / 2f)),
            }, 0.3f);
        }
        #endif
    }
}