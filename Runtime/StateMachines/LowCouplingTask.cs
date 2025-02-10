using System;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    /// <summary>
    /// This structure can be used to wait for the state with specified id.
    /// Unlike UniTask, this structure doest not create new tasks in advance. Thus, it is not mandatory to wait.
    /// This is a safe way to provide the option to wait for the transition to complete when waiting may be optional.
    /// </summary>
    public readonly struct LowCouplingTask
    {
        public static UniTask CompletedWaitState(ulong id, bool throwIfNotFound, Signal cancellationSignal)
        {
            return UniTask.CompletedTask;
        }

        // ReSharper disable once InconsistentNaming
        public static LowCouplingTask Completed =>
            new LowCouplingTask(0, CompletedWaitState);
        private readonly ulong ID;
        private readonly Func<ulong, bool, Signal, UniTask> m_WaitState;
        
        public LowCouplingTask(ulong id, Func<ulong, bool, Signal, UniTask> waitState)
        {
            ID = id;
            m_WaitState = waitState;
        }
        
        public UniTask WaitTransition(bool throwIfNotFound = false, Signal cancellationSignal = default)
        {
            return m_WaitState(ID, throwIfNotFound, cancellationSignal);
        }
        
        public UniTask WaitTransition(Signal cancellationSignal = default)
        {
            return m_WaitState(ID, false, cancellationSignal);
        }

        public UniTask.Awaiter GetAwaiter()
        {
            return m_WaitState(ID, false, default).GetAwaiter();
        }
    }
}