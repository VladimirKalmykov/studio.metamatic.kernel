using System;

namespace Metamatic
{
    [Flags]
    public enum Direction2h
    {
        None = 0,
        Right = 1 << 0,
        Left = 1 << 1
    }
}