using System;
using UnityEngine;

namespace Metamatic
{
    /// <summary>
    /// Use it for classes that are compatible with a specific platform. You can add multiple attributes for multiple platforms.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class PlatformCompatibleAttribute: Attribute
    {
        public readonly RuntimePlatform platform;
        public PlatformCompatibleAttribute(RuntimePlatform platform)
        {
            this.platform = platform;
        }
    }
}