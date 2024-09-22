using System;

namespace Metamatic
{
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct, AllowMultiple = false)]
    public class UseStorageSynchronizerAttribute: System.Attribute
    {
        public readonly Type synchronizerType;
        public UseStorageSynchronizerAttribute(Type synchronizerType)
        {
            this.synchronizerType = synchronizerType;
        }
    }
}