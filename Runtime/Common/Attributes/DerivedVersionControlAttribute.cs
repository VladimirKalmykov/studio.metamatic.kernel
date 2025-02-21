using System;

namespace Metamatic
{
    /// <summary>
    /// Attribute determines that derived classes must have VersionAttribute. It works also in revert logic - if class has VersionAttribute, it must have DerivedVersionControlAttribute in the parent class.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class DerivedVersionControlAttribute: Attribute
    {
        public static bool IsDerivedVersionControl(Type type)
        {
            return type.GetCustomAttributes(typeof(DerivedVersionControlAttribute), true).Length > 0;
        }
    }
}