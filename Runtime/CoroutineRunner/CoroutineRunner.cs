using UnityEngine;

namespace Metamatic
{
    public class CoroutineRunner : MonoBehaviour
    {
        private static CoroutineRunner _Instance;
        // ReSharper disable once InconsistentNaming
        public static CoroutineRunner Instance
        {
            get
            {
                if (_Instance == null)
                {
                    if (!Application.isPlaying)
                    {
                        throw new System.Exception("CoroutineRunner should be created in play mode only");
                    }
                    var obj = new GameObject("__CoroutineRunner__");
                    DontDestroyOnLoad(obj);
                    _Instance = obj.AddComponent<CoroutineRunner>();
                }
                return _Instance;
            }
        }
    }
}