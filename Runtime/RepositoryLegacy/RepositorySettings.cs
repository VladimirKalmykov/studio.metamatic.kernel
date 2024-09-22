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
        public bool enabled;
        public StorageSource remoteStorage;
        public StorageSource localStorage;
    }
    
    [System.Serializable]
    public class StorageSource
    {
        public bool enabled;
        public string storage;
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