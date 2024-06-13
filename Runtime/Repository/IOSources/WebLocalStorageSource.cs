using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    [PlatformCompatible(RuntimePlatform.WebGLPlayer)]
    public class WebLocalStorageSource: IOSource
    {
        public override UniTask<T> Read<T>(string name, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }

        public override UniTask Write<T>(string uri, T value, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }
    }
}