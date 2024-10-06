using System;

namespace Metamatic
{
    [Flags]
    public enum Direction2v
    {
        None = 0,
        Top = 1 << 0,
        Bottom = 1 << 1
    }
}