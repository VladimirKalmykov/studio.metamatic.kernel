using System;
using UnityEngine;

namespace Metamatic.Pathfinder
{
    [Serializable]
    public struct SerializedWaypoint2D
    {
        public Vector2 Position;
        public WaypointType Type;
        public SerializedEdge2D[] Neighbors;
        
        public SerializedWaypoint2D(Vector2 position, WaypointType type, SerializedEdge2D[] neighbors)
        {
            Position = position;
            Type = type;
            Neighbors = neighbors;
        }
    }
}