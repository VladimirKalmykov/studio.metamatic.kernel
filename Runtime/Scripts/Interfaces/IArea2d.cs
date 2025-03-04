using UnityEngine;

namespace Metamatic
{
    public interface IArea2d
    {
        /// <summary>
        /// Возвращает прямоугольник, описывающий 2D-область
        /// </summary>
        Rect rect { get; }
    }
}