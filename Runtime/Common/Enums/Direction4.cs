using System;

namespace Metamatic
{
    /// <summary>
    /// Describe the four cardinal directions.
    /// </summary>
    [Flags]
    public enum Direction4
    {
        None = 0,
        Top = 1 << 0,
        Right = 1 << 1,
        Bottom = 1 << 2,
        Left = 1 << 3
    }
}