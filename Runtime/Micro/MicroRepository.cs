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
        
        public UniTask<T> Read(float timeout = 0f, CancellationToken token = default)
        {
            return source.Read<T>(new Repository.IOOperationConfig(
                address: address,
                timeout: timeout
            ), token);
        }
        
        public UniTask Write(T value, float timeout = 0f, CancellationToken token = default)
        {
            return source.Write(new Repository.IOOperationConfig(
                address: address,
                timeout: timeout
            ), value, token);
        }

        public void Dispose()
        {
            
        }
    }
}