using System;

namespace Metamatic.Framework
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MetamaticCursorToolAttribute: Attribute
    {
        public readonly string PathName;
        public readonly string DisplayName;
        public readonly string Icon;
        public MetamaticCursorToolAttribute(string pathName, string displayName, string icon)
        {
            PathName = pathName;
            DisplayName = displayName;
            Icon = icon;
        }
    }
}