using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metamatic
{
    interface IPlug
    {
        
    }

    interface IPlug<TScheme>: IPlug
    where TScheme: SocketScheme
    {
        
    }
    
    public abstract class Plug: MonoBehaviour, IPlug
    {
        
    }
    
    public abstract class Plug<TScheme>: MonoBehaviour
    where TScheme: SocketScheme, new()
    {
        [ShowInInspector, ReadOnly]
        private Socket<TScheme> m_Socket;
        [SerializeField] public TScheme scheme;
    }
}