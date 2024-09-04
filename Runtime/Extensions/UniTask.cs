using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public class MultipleAwaitableUniTask
    {
        private UniTaskCompletionSource m_CompletionSource;
        public bool IsCompleted { get; private set; } = false;
        public MultipleAwaitableUniTask(UniTask task)
        {
            m_CompletionSource = new UniTaskCompletionSource();
            AwaitTask(task, m_CompletionSource);
        }

        private async void AwaitTask(UniTask task, UniTaskCompletionSource completionSource)
        {
            try
            {
                await task;
                IsCompleted = true;
                completionSource.TrySetResult();
            } catch (Exception e)
            {
                completionSource.TrySetException(e);
            }
        }
        
        /* Support convert to unitsk */
        public static implicit operator UniTask(MultipleAwaitableUniTask awaitable)
        {
            return awaitable.m_CompletionSource.Task;
        }
    }
    public static class UniTaskExtensions
    {
        public static async UniTask<T> ForgetCancellation<T>(this UniTask<T> task)
        {
            try
            {
                return await task;
            }
            catch (OperationCanceledException)
            {
                return default(T);
            }
        }
        
        /// <summary>
        /// Create a special awaitable object that can be awaited multiple times
        /// </summary>
        /// <param name="tasks"></param>
        public static MultipleAwaitableUniTask MultipleAwaitable(this UniTask task)
        {
            return new MultipleAwaitableUniTask(task);
        }
        
        public static UniTask Then(this UniTask task, Action action)
        {
            return task.ContinueWith(action);
        }
        
        public static async UniTask Catch(this UniTask task, Action<Exception> action)
        {
            try
            {
                await task;
            } catch (Exception e)
            {
                action?.Invoke(e);
            }
        }
    }
}