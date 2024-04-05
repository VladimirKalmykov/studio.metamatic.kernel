using System.Threading;
using UnityEngine;

namespace Metamatic
{
    [PlatformCompatible(RuntimePlatform.WebGLPlayer)]
    public class WebLocalStorageSource: IOSource
    {
        public override AsyncReadOperation<T> Read<T>(string name, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }

        public override AsyncWriteOperation<T> Write<T>(string uri, T value, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }
    }
}