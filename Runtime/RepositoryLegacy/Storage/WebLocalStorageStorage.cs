using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    [PlatformCompatible(RuntimePlatform.WebGLPlayer)]
    public class WebLocalStorageLegacy: StorageLegacy
    {
        public override UniTask<(TData data, Metadata metadata, TMetadata customMetadata)> Get<TData, TMetadata>(string address, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }

        public override UniTask Set<TData, TMetadata>(string address, TData value, TMetadata metadata, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }

        public override UniTask Delete<TData>(string address, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }
    }
}