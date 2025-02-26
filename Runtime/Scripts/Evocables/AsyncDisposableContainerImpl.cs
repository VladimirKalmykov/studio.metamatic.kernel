using System;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    /// <summary>
    /// Allows to create a disposable container that can form its content asynchronously from a given input value and initializer function.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    public readonly struct AsyncDisposableContainer<TResult>: IDisposable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AsyncDisposableContainer<TResult> Create<TInput>(Func<Action<TResult>, TInput, Signal, UniTask> initializer,
            TInput input)
        {
            return AsyncDisposableContainerImpl<TResult>.Create(initializer, input);
        }
        private readonly AsyncDisposableContainerImpl<TResult> m_Impl;
        private readonly int m_Version;
        public bool isAlive => m_Impl != null && m_Impl.version == m_Version;
        public AsyncDisposableContainerImpl<TResult> impl => m_Impl != null && m_Impl.version == m_Version ? m_Impl : null;
        public AsyncDisposableContainer(AsyncDisposableContainerImpl<TResult> impl)
        {
            m_Impl = impl;
            m_Version = impl.version;
        }

        public async UniTask<TResult> GetContent(Signal cancellationSignal)
        {
            return await impl.GetContent(cancellationSignal);
        }

        public void Dispose()
        {
            impl?.Return();
        }
    }
    public class AsyncDisposableContainerImpl<TResult>: IResettable
    {
        public static AsyncDisposableContainer<TResult> Create<TInput>(Func<Action<TResult>, TInput, Signal, UniTask> initializer,
            TInput input)
        {
            var container = ParamLessPool<AsyncDisposableContainerImpl<TResult>>.GetShared();
            container.Initialize(initializer, input).Forget();
            
            return new AsyncDisposableContainer<TResult>(container);
        }
        
        private async UniTaskVoid Initialize<TInput>(Func<Action<TResult>, TInput, Signal, UniTask> initializer, TInput input)
        {
            try
            {
                m_DisposeSignalSource = new SignalSource();
                m_CompletionSource = SafeUniTaskCompletionSource.Create();
                await initializer(SetResult, input, m_DisposeSignalSource.Signal);
            }
            catch (OperationCanceledException)
            {
                m_CompletionSource.TrySetCanceled();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                m_CompletionSource.TrySetException(e);
            }
            finally
            {
                m_CompletionSource.Task.Forget(); // Release the task source
            }
        }
        
        protected SafeUniTaskCompletionSource m_CompletionSource = default;
        protected TResult Result;
        protected SignalSource m_DisposeSignalSource = default;
        protected bool Resolved;
        public int version { get; private set; }
        
        private void SetResult(TResult result)
        {
            Resolved = true;
            m_CompletionSource.TrySetResult();
            m_CompletionSource.Task.Forget(); // Release the task source
            Result = result;
        }
        
        public async UniTask<TResult> GetContent(Signal cancellationSignal)
        {
            if (Resolved)
            {
                return Result;
            }
            var winnerIndex = await UniTask.WhenAny(m_CompletionSource.Task, cancellationSignal.ToUniTask());
            if (winnerIndex == 0)
            {
                return Result;
            }
            throw new OperationCanceledException();
        }

        internal void Return()
        {
            ParamLessPool<AsyncDisposableContainerImpl<TResult>>.ReturnShared(this);
        }

        /// <summary>
        /// This method is called automatically when the object is returned to the pool
        /// </summary>
        public void Reset()
        {
            unchecked
            {
                version++;
            }
            m_DisposeSignalSource.Dispose();
            m_CompletionSource.TrySetCanceled();
            m_CompletionSource.Task.Forget();
            Resolved = false;
        }
    }
    
}