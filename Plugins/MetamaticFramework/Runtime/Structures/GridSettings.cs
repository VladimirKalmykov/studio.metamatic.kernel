using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Metamatic.Framework
{
    [Serializable]
    public struct Grid2DSettings
    {
        [Serializable]
        public enum AppearanceType
        {
            Lines,
            DottedLines,
            Dots
        }
        public Vector2 gridSize;
        public Vector2 cellSize;
        public Vector3 rotation;
        public Vector3 offset;
        public Color color;
        public AppearanceType appearance;
        public float weight;
        /// <summary>
        /// Using if appearance is DottedLines
        /// </summary>
        public float screenSpaceSize;
    }
}