using UnityEngine;

namespace Metamatic
{
    public class God: MonoBehaviour
    {
        private static GameObject _Instance;
        public static GameObject Instance
        {
            get
            {
                if (_Instance == null)
                {
                    var go = GameObject.Find("God");
                    go.AddComponent<God>();
                    DontDestroyOnLoad(go);
                    _Instance = go;
                }
                return _Instance;
            }
        }
    }
}