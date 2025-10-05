using System;
using Cysharp.Threading.Tasks;
using Metamatic;

namespace Metamatic
{
    public readonly struct DisposableTask: IDisposable
    {
        private readonly UniTask m_Task;
        private readonly Action m_DisposeAction;
        public DisposableTask(UniTask task, Action disposeAction)
        {
            m_Task = task;
            m_DisposeAction = disposeAction;
        }

        public void Dispose()
        {
            m_DisposeAction?.Invoke();
        }
        
        public UniTask AsUniTask()
        {
            return m_Task;
        }
        
        public static implicit operator UniTask(DisposableTask disposableTask)
        {
            return disposableTask.m_Task;
        }
        
        public UniTask.Awaiter GetAwaiter()
        {
            return m_Task.GetAwaiter();
        }
    }

    public readonly struct DisposableTask<T> : IDisposable
    {
        private readonly UniTask<T> m_Task;
        private readonly Action m_DisposeAction;

        public DisposableTask(UniTask<T> task, Action disposeAction)
        {
            m_Task = task;
            m_DisposeAction = disposeAction;
        }
        
        public void Dispose()
        {
            m_DisposeAction?.Invoke();
        }

        public UniTask<T> AsUniTask()
        {
            return m_Task;
        }

        public static implicit operator UniTask<T>(DisposableTask<T> disposableTask)
        {
            return disposableTask.m_Task;
        }

        public UniTask<T>.Awaiter GetAwaiter()
        {
            return m_Task.GetAwaiter();
        }
    }
}