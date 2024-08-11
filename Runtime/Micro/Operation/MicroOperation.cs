using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public readonly struct MicroOperation: IDisposableOperation
    {
        public static MicroOperation Call(InternalMicroOperation.AsyncAction action)
        {
            return InternalMicroOperation.Call(action);
        }
        
        public static MicroOperation Run(InternalMicroOperation.AsyncOperationAction action)
        {
            return InternalMicroOperation.Run(action);
        }
        
        private readonly InternalMicroOperation m_Operation;
        private readonly int m_Id;
        
        public MicroOperation(InternalMicroOperation operation)
        {
            m_Operation = operation;
            m_Id = m_Operation.Inquire();
        }

        private async UniTask AnyDispose(bool async)
        {
            await m_Operation.Release(m_Id, async);
        }

        public void Dispose()
        {
            AnyDispose(false).Forget();
        }
        
        public static implicit operator DisposableAction(MicroOperation operation)
        {
            return new DisposableAction(operation.Dispose);
        }

        public IDisposable ToIDisposable()
        {
            return new DisposableAction(Dispose);
        }

        public UniTask DisposeAsync()
        {
            return AnyDispose(true);
        }
    }
}