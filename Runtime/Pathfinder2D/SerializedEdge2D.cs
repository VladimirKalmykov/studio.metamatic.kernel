using System;

namespace Metamatic.Pathfinder
{
    [Serializable]
    public readonly struct SerializedEdge2D
    {
        public readonly int TargetIndex;
        public readonly float Cost;

        public SerializedEdge2D(int targetIndex, float cost)
        {
            TargetIndex = targetIndex;
            Cost = cost;
        }
    }
}