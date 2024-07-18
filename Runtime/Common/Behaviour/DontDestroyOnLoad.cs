using UnityEngine;

namespace Metamatic
{
    public class DontDestroyOnLoad: MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}