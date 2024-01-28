using System.Threading;
using UnityEngine;

namespace Metamatic
{
    public class Store<T>: AsyncBehaviour
    where T: struct
    
    {
        [SerializeField] private string storeName;

        protected override async void RunAsync(CancellationToken cancellationToken)
        {
            using var store = Repository.GetStore<T>(storeName);
        }
    }
}