using System;

namespace Metamatic.Framework
{
    [Flags]
    public enum WidgetApplicability
    {
        None = 0,
        /// <summary>
        /// Widget is applicable in the editor
        /// </summary>
        Editor = 1 << 1,
        /// <summary>
        /// Widget is applicable in the game overlay
        /// </summary>
        Viewport = 1 << 2,
        Both = Editor | Viewport
    }
}