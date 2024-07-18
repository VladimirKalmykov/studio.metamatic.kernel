using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public class LocalMemoryStorage: Storage
    {
        private Dictionary<(Type, string), (object, Storage.Metadata)> m_Memory = new ();
        public override UniTask<(T, Metadata, TMetadata)> Get<T, TMetadata>(string address, CancellationToken token)
        {
            if (!enabled)
            {
                throw new Storage.DisabledException();
            }
            var taskCompletionSource = new UniTaskCompletionSource<(T, Metadata, TMetadata)>();
            var type = typeof(T);
            if (m_Memory.TryGetValue((type, address), out var value))
            {
                var metadataWithCustom = (Metadata<TMetadata>)value.Item2;
                var customMetadata = metadataWithCustom.custom;
                taskCompletionSource.TrySetResult(((T)value.Item1, (Metadata)value.Item2, customMetadata));
            }
            else
            {
                taskCompletionSource.TrySetException(new Storage.NotFoundException("Key not found"));
            }

            return taskCompletionSource.Task;
        }

        public override UniTask Set<T, TMetadata>(string address, T value, TMetadata customMetadata, CancellationToken token)
        {
            if (!enabled)
            {
                throw new Storage.DisabledException();
            }
            var taskCompletionSource = new UniTaskCompletionSource();
            var type = typeof(T);
            var metadata = SerializableDataPackage<T, TMetadata>.GetMetadata(value, customMetadata);
            
            m_Memory[(type, address)] = (value, metadata);
            taskCompletionSource.TrySetResult();
            return taskCompletionSource.Task;
        }

        public override UniTask Delete<TData>(string address, CancellationToken token)
        {
            if (!enabled)
            {
                throw new Storage.DisabledException();
            }
            var taskCompletionSource = new UniTaskCompletionSource();
            var type = typeof(TData);
            if (m_Memory.Remove((type, address)))
            {
                taskCompletionSource.TrySetResult();
            }
            else
            {
                taskCompletionSource.TrySetException(new Storage.NotFoundException("Key not found"));
            }

            return taskCompletionSource.Task;
        }
    }
}