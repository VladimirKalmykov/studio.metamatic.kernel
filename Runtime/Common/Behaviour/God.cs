using UnityEngine;

namespace Metamatic
{
    public class God: MonoBehaviour
    {
        private static God _Instance;
        public static God Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new GameObject("God").AddComponent<God>();
                    DontDestroyOnLoad(_Instance.gameObject);
                }
                return _Instance;
            }
        }
    }
}