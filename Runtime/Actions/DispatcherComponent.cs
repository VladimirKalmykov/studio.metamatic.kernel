using UnityEngine;

namespace Metamatic.Actions
{
    [DisallowMultipleComponent]
    public class DispatcherComponent: MonoBehaviour
    {
        private Dispatcher m_Dispatcher;
        public Dispatcher dispatcher => m_Dispatcher ??= new Dispatcher();

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}