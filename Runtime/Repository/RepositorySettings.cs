using UnityEngine;

namespace Metamatic
{
    [System.Serializable]
    public abstract class IOSourceSettings
    {
        
    }
    
    [System.Serializable]
    public class PlatformSource
    {
        public RuntimePlatform platform;
        public bool enableRemoteStorage;
        public bool enableLocalStorage;
        /// <summary>
        /// Class name of the source
        /// </summary>
        public string remoteStorage;

        public string localStorage;
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