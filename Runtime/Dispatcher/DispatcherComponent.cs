using System;
using UnityEngine;

namespace Metamatic
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

        private void OnDestroy()
        {
            m_Dispatcher?.Dispose();
        }
    }
}