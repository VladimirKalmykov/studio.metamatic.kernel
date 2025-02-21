using System;
using System.Linq;
using System.Reflection;

namespace Metamatic
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class VersionAttribute: Attribute
    {
        private string m_Value;
        public string value => m_Value;
        private Type m_RelatedType;
        public Type relatedType => m_RelatedType;
        /// <summary>
        /// Determines that this version is the latest for the specific type
        /// </summary>
        private bool m_Latest;
        public bool latest => m_Latest;

        public VersionAttribute(string value)
        {
            m_Value = value;
            m_Latest = true;
        }
        
        public VersionAttribute(string value, Type relatedType)
        {
            m_Value = value;
            m_Latest = false;
            m_RelatedType = relatedType;
        }
        
        /// <summary>
        /// Get version for the specific type
        /// </summary>
        /// <param name="byDefault">Version by the default (0)</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static string GetVersion<T>(string byDefault = "0")
        {
            Type type = typeof(T);
            
            var versionAttributes = type.GetCustomAttributes<VersionAttribute>();
            var latestVersion = versionAttributes.FirstOrDefault(x => x.latest);
            
            return latestVersion != null ? latestVersion.value : byDefault;
        }
        
        public static string GetVersion(Type type, string byDefault = "0")
        {
            var versionAttributes = type.GetCustomAttributes<VersionAttribute>();
            var latestVersion = versionAttributes.FirstOrDefault(x => x.latest);
            
            return latestVersion != null ? latestVersion.value : byDefault;
        }
        
        /// <summary>
        /// Return type for the specific version
        /// </summary>
        /// <example>
        /// class TestVersion13 {}
        /// [Version("1.2")]
        /// [Version("1.3", typeof(TestVersion13))]
        /// class Test {}
        /// </example>
        /// <param name="version"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Type GetTypeForSpecifiedVersion<T>(string version)
        {
            Type type = typeof(T);
            
            var versionAttributes = type.GetCustomAttributes<VersionAttribute>();
            var relatedVersion = versionAttributes.FirstOrDefault(x => x.value == version);
            
            return relatedVersion?.relatedType;
        }
        
        public static Type GetTypeForSpecifiedVersion(Type type, string version)
        {
            var versionAttributes = type.GetCustomAttributes<VersionAttribute>();
            var relatedVersion = versionAttributes.FirstOrDefault(x => x.value == version);
            
            return relatedVersion?.relatedType ?? null;
        }
    }
}