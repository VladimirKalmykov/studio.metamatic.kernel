using System;
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
        private static SignedIndexedPool<SafeUniTaskCompletionSource> s_WaitStatePool = new (100);
        public static UniTask CompletedWaitState(Token96Bit token, bool throwIfNotFound, Signal cancellationSignal)
        {
            return UniTask.CompletedTask;
        }

        /**
         * This method creates a new LowCouplingTask from the UniTask. This method ensures that the task is tracked and removed from the pool when it is completed.
         * Under the hood
         */
        public static LowCouplingTask From(UniTask task)
        {
            var source = SafeUniTaskCompletionSource.Create();
            var token = s_WaitStatePool.Add(source);
            
            InternalTrackTaskState(task, token, source).Forget();
            
            return new LowCouplingTask(token, WaitState);
        }
        
        public static UniTask WaitState(Token96Bit token, bool throwIfNotFound, Signal cancellationSignal)
        {
            if (s_WaitStatePool.TryGet(token, out var source))
            {
                return source.Task;
            }

            if (throwIfNotFound)
            {
                throw new InvalidOperationException($"Wait state with id {token.ID} not found.");
            }
            return UniTask.CompletedTask;
        }
        
        private static async UniTaskVoid InternalTrackTaskState(UniTask task, Token96Bit token, SafeUniTaskCompletionSource source)
        {
            try
            {
                await task;
                source.TrySetResult();
            }
            catch (Exception e)
            {
                source.TrySetException(e);
            }
            finally
            {
                s_WaitStatePool.Remove(token);
            }
        }

        // ReSharper disable once InconsistentNaming
        public static LowCouplingTask Completed =>
            new LowCouplingTask(Token96Bit.Empty, CompletedWaitState);
        private readonly Token96Bit m_Token;
        private readonly Func<Token96Bit, bool, Signal, UniTask> m_WaitState;
        
        public LowCouplingTask(Token96Bit token, Func<Token96Bit, bool, Signal, UniTask> waitState)
        {
            m_Token = token;
            m_WaitState = waitState;
        }
        
        public UniTask WaitTransition(bool throwIfNotFound = false, Signal cancellationSignal = default)
        {
            return m_WaitState(m_Token, throwIfNotFound, cancellationSignal);
        }
        
        public UniTask WaitTransition(Signal cancellationSignal = default)
        {
            return m_WaitState(m_Token, false, cancellationSignal);
        }

        public UniTask.Awaiter GetAwaiter()
        {
            return m_WaitState(m_Token, false, default).GetAwaiter();
        }
    }
}