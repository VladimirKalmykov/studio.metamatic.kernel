using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Metamatic.MicroBehaviours
{
    public partial class CameraFOVTracker2d: MicroBehaviour
    {
        [BurstCompile]
        public struct TrackAreasJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Rect> allRects;
            [ReadOnly] public NativeArray<Rect> areaRects;
            [ReadOnly] public NativeArray<FovBoundsType> acquiredBoundsTypesBinaryMaskArray;
            [ReadOnly] public NativeArray<FovBoundsType> mapBoundsTypesToRectIndexesBoundTypes;
            [ReadOnly] public NativeArray<int> mapBoundsTypeToRectsStartIndex;
            [ReadOnly] public NativeArray<int> mapBoundsTypeToRectsEndIndex;
            [ReadOnly] public NativeArray<int> mapBoundsTypeToRectsRectIndexes;
            
            public NativeArray<FovBoundsType> resultBoundsTypeMaskArray;
            public NativeArray<ulong> intersectedRectsResult;
            public void Execute(int index)
            {
                ulong intersectedRects = 0;
                var mask = acquiredBoundsTypesBinaryMaskArray[index];
                FovBoundsType resultBoundsTypeMask = 0;
                foreach (FovBoundsType boundsType in mapBoundsTypesToRectIndexesBoundTypes)
                {
                    if ((mask & boundsType) != 0)
                    {
                        if (BoundsOverlapsArea(allRects, boundsType, areaRects[index], ref intersectedRects))
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
                intersectedRectsResult[index] = intersectedRects;
            }
            
            private bool BoundsOverlapsArea(NativeArray<Rect> rects, FovBoundsType boundsType, Rect areaRectRepresentation, ref ulong intersectedRects)
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
                    if (rects[rectIndex].Overlaps(areaRectRepresentation, true))
                    {
                        intersectedRects |= 1UL << rectIndex;
                    }
                }
                return intersectedRects > 0;
            }
        }
    }
}