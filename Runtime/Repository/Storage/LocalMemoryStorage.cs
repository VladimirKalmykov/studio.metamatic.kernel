using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public class LocalMemoryStorage: Storage
    {
        private Dictionary<(Type, string), (object, Storage.Metadata)> m_Memory = new ();
        public override UniTask<(T, Storage.Metadata)> Get<T>(string address, CancellationToken token)
        {
            var taskCompletionSource = new UniTaskCompletionSource<(T, Storage.Metadata)>();
            var type = typeof(T);
            if (m_Memory.TryGetValue((type, address), out var value))
            {
                taskCompletionSource.TrySetResult(((T)value.Item1, value.Item2));
            }
            else
            {
                taskCompletionSource.TrySetException(new Storage.NotFoundException("Key not found"));
            }

            return taskCompletionSource.Task;
        }

        public override UniTask Set<T>(string address, T value, CancellationToken token)
        {
            var taskCompletionSource = new UniTaskCompletionSource();
            var type = typeof(T);
            var metadata = SerializableDataPackage<T>.GetMetadata(value);
            
            m_Memory[(type, address)] = (value, metadata);
            taskCompletionSource.TrySetResult();
            return taskCompletionSource.Task;
        }
    }
}