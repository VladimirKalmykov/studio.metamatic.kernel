using System;
using System.Linq;

namespace Metamatic {
    public static class AutomationUtils {
        /// <summary>
        /// Mix two structs and return new one
        /// </summary>
        /// <param name="config"></param>
        /// <param name="otherConfig"></param>
        /// <returns></returns>
        public static object AssignStruct(object  config, object otherConfig) {
            Type currentType = config.GetType();
            Type otherType = otherConfig.GetType();

            object nextConfig = Activator.CreateInstance(currentType);
            Type nextType = nextConfig.GetType();

            foreach (var propertyInfo in currentType.GetProperties().Where(prop => prop.CanRead && prop.CanWrite))
            {
                if (otherType.GetProperty(propertyInfo.Name) != null) {
                    propertyInfo.SetValue(nextConfig, propertyInfo.GetValue(otherConfig));
                } else {
                    propertyInfo.SetValue(nextConfig, propertyInfo.GetValue(config));
                }
            }

            return (object)nextConfig;
        }
    }
}