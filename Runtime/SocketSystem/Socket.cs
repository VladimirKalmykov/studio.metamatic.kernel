using UnityEngine;

namespace Metamatic
{
    public class Socket: MonoBehaviour
    {
        
    }
    
    public class Socket<TScheme>: MonoBehaviour
    where TScheme: SocketScheme
    {
        [SerializeField] public TScheme scheme;
    }
}