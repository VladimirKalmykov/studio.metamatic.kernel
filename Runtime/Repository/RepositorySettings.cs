using UnityEngine;
using UnityEngine.Serialization;

namespace Metamatic
{
    [System.Serializable]
    public class PlatformSource
    {
        public RuntimePlatform platform;
        /// <summary>
        /// Class name of the source
        /// </summary>
        public string sourceClassName;
        public IOSourceSettings settings;
    }
    
    /// <summary>
    /// Do not create this object manually. It is used for repository settings.
    /// </summary>
    public class RepositorySettings: ScriptableObject
    {
        public PlatformSource[] platforms;
    }
}