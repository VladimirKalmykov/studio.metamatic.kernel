using System.Collections.Generic;
using UnityEngine;

namespace Metamatic
{
    public readonly struct ColorPalette
    {
        public readonly HashSet<Color> Colors;
        
        public ColorPalette(HashSet<Color> colors)
        {
            Colors = colors;
        }
        
        public Color GetRandomColor()
        {
            var colors = new List<Color>(Colors);
            return colors[Random.Range(0, colors.Count)];
        }
    }
}