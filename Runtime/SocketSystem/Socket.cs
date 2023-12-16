using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metamatic
{
    public class Socket: MonoBehaviour
    {
        
    }
    
    public class Socket<TScheme>: MonoBehaviour
    where TScheme: SocketScheme
    {
        [ShowInInspector, ReadOnly]
        private List<Plug<TScheme>> m_Plugs;
        [SerializeField] public TScheme scheme;

        public Action ConnectPlug(Plug<TScheme> plug)
        {
            return () => { };
        }
    }
}