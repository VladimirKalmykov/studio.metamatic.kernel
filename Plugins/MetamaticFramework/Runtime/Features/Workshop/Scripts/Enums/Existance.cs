using System;

namespace Metamatic.Framework.Workshop
{
    [Flags]
    public enum ExistenceType
    {
        /// <summary>
        /// Not exists.
        /// </summary>
        None = 0,
        /// <summary>
        /// Exists in material world
        /// </summary>
        Material = 1 << 0,
        /// <summary>
        /// Exists in virtual world
        /// </summary>
        Virtual = 1 << 1,
    }
}