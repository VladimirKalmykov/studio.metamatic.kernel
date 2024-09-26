using System;
using System.Collections.Generic;
using UnityEngine;

namespace Metamatic.Pathfinder
{
    public sealed class Waypoint2D
    {
        public Vector2 Position; // Место нахождения вейпоинта
        public WaypointType Type; // Тип вейпоинта
        public readonly List<Edge2D> Neighbors; // Соседи (связи)
        
        /// <summary>
        /// Helper for generating edges
        /// </summary>
        public bool[] sectorsOccupied;

        public int id;

        public Waypoint2D(Vector2 position, WaypointType type)
        {
            Position = position;
            Type = type;
            Neighbors = new List<Edge2D>();
        }

        public void AddNeighbor(Waypoint2D neighbor, float cost)
        {
            Neighbors.Add(new Edge2D(neighbor, cost));
        }
    }
}