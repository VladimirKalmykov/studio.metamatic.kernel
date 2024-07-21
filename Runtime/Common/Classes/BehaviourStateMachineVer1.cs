using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    /// <summary>
    /// State machine help to implement state machine pattern: when some object can be in different states and behave differently in each state depending on previous state and payload.
    /// </summary>
    /// <typeparam name="TState">State type</typeparam>
    /// <typeparam name="TResult">Data type that state handler save after it's done and pass to next state handler</typeparam>
    public class BehaviourStateMachineVer1<TState, TResult>: IDisposable
    {
        public delegate UniTask<TResult> StateChangeHandler(TState state, TState prevState, TResult prevResult, CancellationToken cancellationToken);
        
        private readonly StateChangeHandler m_Handler;
        private TState m_CurrentState;
        private TResult m_PrevResult;
        private CancellationTokenSource m_CancellationTokenSource;
        public TState state => m_CurrentState;
        
        public BehaviourStateMachineVer1(StateChangeHandler handler, TState defaultState = default)
        {
            this.m_Handler = handler;
            m_CurrentState = defaultState;
        }
        
        public async UniTask<TResult> SetState(TState state, CancellationToken cancellationToken)
        {
            if (m_CancellationTokenSource != null)
            {
                m_CancellationTokenSource.Cancel();
                m_CancellationTokenSource.Dispose();
            }
            var prevState = m_CurrentState;
            m_CurrentState = state;
            m_CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            try {
                var result = await m_Handler(m_CurrentState, prevState, m_PrevResult, m_CancellationTokenSource.Token);
                m_PrevResult = result;
                return result;
            }
            catch (OperationCanceledException)
            {
                m_PrevResult = default;
                throw;
            }
        }

        public void Dispose()
        {
            m_CancellationTokenSource?.Cancel();
            m_CancellationTokenSource?.Dispose();
        }
    }
    
    public class BehaviourStateMachine<TState>: BehaviourStateMachineVer1<TState, object>
    {
        public BehaviourStateMachine(StateChangeHandler handler, TState defaultState = default) : base(handler, defaultState)
        {
        }
    }
}