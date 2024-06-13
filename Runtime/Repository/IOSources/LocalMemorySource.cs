using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public class LocalMemorySource: IOSource
    {
        private Dictionary<(Type, string), string> m_Memory = new ();
        public override UniTask<T> Read<T>(string name, CancellationToken token)
        {
            var taskCompletionSource = new UniTaskCompletionSource<T>();
            var type = typeof(T);
            if (m_Memory.TryGetValue((type, name), out var value))
            {
                taskCompletionSource.TrySetResult(Deserialize<T>(value));
            }
            else
            {
                taskCompletionSource.TrySetException(new IOSource.NotFoundException("Key not found"));
            }

            return taskCompletionSource.Task;
        }

        public override UniTask Write<T>(string name, T value, CancellationToken token)
        {
            var taskCompletionSource = new UniTaskCompletionSource();
            var type = typeof(T);
            m_Memory[(type, name)] = Serialize<T>(value);
            taskCompletionSource.TrySetResult();
            return taskCompletionSource.Task;
        }
    }
}