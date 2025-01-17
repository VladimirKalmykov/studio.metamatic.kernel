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
        
        /// <summary>
        /// Evaluate the task and invoke the action at the end
        /// </summary>
        /// <param name="task"></param>
        /// <param name="action"></param>
        /// <param name="ignoreCancellation"></param>
        public static async void Finally(this UniTask task, Action<Exception> action, bool ignoreCancellation = true)
        {
            Exception exception = null;
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                if (!ignoreCancellation)
                {
                    exception = new OperationCanceledException();
                }
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
               action?.Invoke(exception); 
            }
        }
        
        /// <summary>
        /// Convert uni task to cancellation signal, that will be cancelled when the task is finished
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public static Signal ToCancellationSignal(this UniTask task)
        {
            var css = SignalSource.Create();
            ConvertUniTaskToCancellationSignal(task, css).Forget();
            return css.Signal;
        }

        private static async UniTaskVoid ConvertUniTaskToCancellationSignal(UniTask task, SignalSource signalSource)
        {
            try
            {
                await task;
            }
            catch (Exception)
            {
                #if DEBUG
                Console.Exception("UniTaskExtensions", "Task was failed");                
                #endif
                // Skip
            }
            finally
            {
                signalSource.TryRaise();
            }
        }
    }
}