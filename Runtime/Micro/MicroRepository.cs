using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public class MicroRepository<T>: IDisposable
    {
        public enum DataSource
        {
            Default,
            Fallback,
            Persistent,
        }
        
        public readonly Repository source;
        public readonly string address;
        
        public MicroRepository(string address)
        {
            source = Repository.global;
            this.address = address;
        }
        
        public MicroRepository(Repository source, string address)
        {
            this.source = source;
            this.address = address;
        }
        
        public UniTask<T> Read(CancellationToken token = default)
        {
            return source.Read<T>(address, token);
        }
        
        public UniTask Write(T value, float timeout = 0f, CancellationToken token = default)
        {
            return source.Write(address, value, token);
        }

        public void Dispose()
        {
            
        }
    }
}