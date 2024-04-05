using System.Collections.Generic;
using System.Threading;

namespace Metamatic
{
    public class LocalMemorySource: IOSource
    {
        private Dictionary<string, string> m_Memory = new Dictionary<string, string>();
        public override AsyncReadOperation<T> Read<T>(string name, CancellationToken token)
        {
            var operationSource = new AsyncReadOperationSource<T>();
            if (m_Memory.TryGetValue(name, out var value))
            {
                operationSource.SetRawValue(value);
            }
            else
            {
                operationSource.SetError(new IOSource.NotFoundException("Key not found"));
            }
            return operationSource.Operation;
        }

        public override AsyncWriteOperation<T> Write<T>(string uri, T value, CancellationToken token)
        {
            var operationSource = new AsyncWriteOperationSource<T>();
            m_Memory[uri] = operationSource.PrepareValue(value);
            operationSource.SetCompleted();
            return operationSource.Operation;
        }
    }
}