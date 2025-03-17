using System;

namespace Metamatic.Framework
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class VirtualizationAttribute: Attribute
    {
        public Type type { get; }
        public VirtualizationAttribute(Type type)
        {
            this.type = type;
        }
    }
}