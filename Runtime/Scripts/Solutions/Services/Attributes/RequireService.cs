using System;
using System.Linq;

namespace Metamatic
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class RequireServiceAttribute : System.Attribute
    {
        public RequireServiceAttribute(Type serviceType)
        {
            this.serviceType = serviceType;
            
            /*
             * Ensure that serviceType is derived from Service
             */
            if (!serviceType.IsSubclassOf(typeof(Service))) throw new ArgumentException($"serviceType must be derived from Service");
        }
        
        public static Type[] GetRequiredServices(Type target)
        {
            return target
                .GetCustomAttributes(typeof(RequireServiceAttribute), false)
                .Cast<RequireServiceAttribute>()
                .Select(attr => attr.serviceType)
                .ToArray();
        }

        public Type serviceType { get; }
    }
}