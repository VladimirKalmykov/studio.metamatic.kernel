using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    [PlatformCompatible(RuntimePlatform.WebGLPlayer)]
    public class WebLocalStorage: Storage
    {
        public override UniTask<(T, Storage.Metadata)> Get<T>(string address, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }

        public override UniTask Set<T>(string address, T value, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }
    }
}