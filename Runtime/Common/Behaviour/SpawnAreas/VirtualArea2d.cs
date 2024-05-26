using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public abstract class VirtualArea2d: VirtualArea
    {
        public bool overlapped { get; set; }
        /// <summary>
        /// Area size of the shape of the area
        /// </summary>
        public abstract float size { get; }
        /// <summary>
        /// Get random position on the area
        /// </summary>
        /// <returns></returns>
        public abstract Vector2 GetRandomPosition(bool local = false);
        /// <summary>
        /// Get random position on the area that is inside the given rect
        /// </summary>
        /// <param name="otherRect"></param>
        /// <returns></returns>
        public abstract Vector2 GetRandomPosition(Rect otherRect);
        /// <summary>
        /// If the area overlaps with the given rect
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public abstract bool Overlaps(Rect rect);
        public abstract float OverlapsAreaSize(Rect rect);
    }
}