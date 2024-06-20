using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Metamatic.MicroBehaviours
{
    public partial class CameraFOVTracker2d: MicroBehaviour
    {
        [BurstCompile]
        public struct TrackObjectsJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Rect> allRects;
            [ReadOnly] public NativeArray<Vector2> trackedObjectPositions;
            [ReadOnly] public NativeArray<FovBoundsType> acquiredBoundsTypesBinaryMaskArray;
            [ReadOnly] public NativeArray<FovBoundsType> mapBoundsTypesToRectIndexesBoundTypes;
            [ReadOnly] public NativeArray<int> mapBoundsTypeToRectsStartIndex;
            [ReadOnly] public NativeArray<int> mapBoundsTypeToRectsEndIndex;
            [ReadOnly] public NativeArray<int> mapBoundsTypeToRectsRectIndexes;
            
            public NativeArray<FovBoundsType> resultBoundsTypeMaskArray;
            public void Execute(int index)
            {
                var mask = acquiredBoundsTypesBinaryMaskArray[index];
                FovBoundsType resultBoundsTypeMask = 0;
                foreach (FovBoundsType boundsType in mapBoundsTypesToRectIndexesBoundTypes)
                {
                    if ((mask & boundsType) != 0)
                    {
                        if (BoundsContainsPoint(allRects, boundsType, trackedObjectPositions[index]))
                        {
                            resultBoundsTypeMask |= boundsType;
                        }
                        else
                        {
                            resultBoundsTypeMask &= ~boundsType;
                        }
                    }
                }

                resultBoundsTypeMaskArray[index] = resultBoundsTypeMask;
            }
            
            private bool BoundsContainsPoint(NativeArray<Rect> rects, FovBoundsType boundsType, Vector2 trackedObjectPosition)
            {
                var boundTypeKeyIndex = 0;
                for (var i = 0; i < mapBoundsTypesToRectIndexesBoundTypes.Length; i++)
                {
                    if (mapBoundsTypesToRectIndexesBoundTypes[i] == boundsType)
                    {
                        boundTypeKeyIndex = i;
                        break;
                    }
                }
                var start = mapBoundsTypeToRectsStartIndex[boundTypeKeyIndex];
                var end = mapBoundsTypeToRectsEndIndex[boundTypeKeyIndex];
                for (var i = start; i <= end; i++)
                {
                    var rectIndex = mapBoundsTypeToRectsRectIndexes[i];
                    if (rects[rectIndex].Contains(trackedObjectPosition))
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }
}