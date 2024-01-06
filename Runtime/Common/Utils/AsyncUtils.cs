using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public enum TaskCompletionReason
    {
        Renew,
        CancellationToken
    }
    public static class AsyncUtils
    {
        public enum FrameType
        {
            Update,
            FixedUpdate,
            LateUpdate
        }

        private static BehaviourTicker ticker
        {
            get;
            set;
        }

        public static BehaviourTicker GetTicker()
        {
            if (ticker != null) return ticker;
            ticker = GameObject.Find("__CommonBahaviourTicker__")?.GetComponent<BehaviourTicker>();
            if (ticker == null)
            {
                ticker = new GameObject("__CommonBahaviourTicker__", new[] {typeof(BehaviourTicker)})
                    .GetComponent<BehaviourTicker>();
            }

            return ticker;
        }

        public static IDisposable RunEveryFrame(Action callback, BehaviourTicker.FrameType frameType = BehaviourTicker.FrameType.Update)
        {
            return GetTicker().EveryFrame(callback, frameType);
        }

        public static CancellationToken TaskToCancellationToken(Task task, CancellationToken anotherCancellationToken = default)
        {
            var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(anotherCancellationToken);
            AwaitCallback(task, () =>
            {
                tokenSource.Cancel();
            });
            return tokenSource.Token;
        }

        public static async void AwaitCallback(Task awaitable, Action callback, bool allowCancellation = false)
        {
            try
            {
                await awaitable;
            }
            catch (Exception ex)
            {
                // ReSharper disable once UseNegatedPatternInIsExpression
                if (!allowCancellation || !(ex is OperationCanceledException))
                    throw;
            }
            finally
            {
                callback();
            }
        }
        
        public static async void AwaitCallback(UniTask awaitable, Action callback, bool allowCancellation = false)
        {
            try
            {
                await awaitable;
            }
            catch (Exception ex)
            {
                // ReSharper disable once UseNegatedPatternInIsExpression
                if (!allowCancellation || !(ex is OperationCanceledException))
                    throw;
            }
            finally
            {
                callback();
            }
        }
        
        public static async void AwaitCallback(UniTask awaitable, Action callback, Action<Exception> errorHandler, bool allowCancellation = false)
        {
            try
            {
                await awaitable;
            }
            catch (Exception ex)
            {
                // ReSharper disable once UseNegatedPatternInIsExpression
                if (!allowCancellation || !(ex is OperationCanceledException))
                {
                    errorHandler(ex);
;                }
            }
            finally
            {
                callback();
            }
        }
        
        /// <summary>
        /// Accept cancellationTokenSource and immediatelly cancel it in order to create new one.
        /// </summary>
        /// <param name="tokenSource"></param>
        /// <returns></returns>
        public static CancellationTokenSource RenewTokenSource(ref CancellationTokenSource tokenSource)
        {
            if (tokenSource != null && !tokenSource.IsCancellationRequested)
            {
                tokenSource.Cancel();
            }
            tokenSource = new CancellationTokenSource();
            return tokenSource;
        }
        
        /// <summary>
        /// Accept cancellationTokenSource and immediatelly cancel it in order to create new one. New token will be passed to callback function.
        /// </summary>
        /// <param name="tokenSource"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static Task RenewTokenSource(ref CancellationTokenSource tokenSource, Func<CancellationToken, Task> callback)
        {
            if (tokenSource != null && !tokenSource.IsCancellationRequested)
            {
                tokenSource.Cancel();
            }

            tokenSource = new CancellationTokenSource();
            return callback(tokenSource.Token);
        }
        
        /// <summary>
        /// Renew task completion source. If previous task is not completed, it will be finished with default value.
        /// </summary>
        /// <param name="taskCompletionSource"></param>
        /// <param name="cancellationToken">The task will be finished if cancellation token will be triggered</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static UniTask<T> RenewTask<T>(ref UniTaskCompletionSource<T> taskCompletionSource, CancellationToken cancellationToken)
        {
            if (taskCompletionSource != null && !taskCompletionSource.Task.Status.IsCompleted())
            {
                taskCompletionSource.TrySetResult(default);
            }
            
            taskCompletionSource = new UniTaskCompletionSource<T>();
            
            if (cancellationToken.IsCancellationRequested)
            {
                taskCompletionSource.TrySetResult(default);
            }
            else
            {
                var memTaskCompletionSource = taskCompletionSource;
                var registration = cancellationToken.Register(() =>
                {
                    memTaskCompletionSource.TrySetResult(default);
                });
                taskCompletionSource.Task.GetAwaiter().OnCompleted(() =>
                {
                    registration.Dispose();
                });
            }

            return taskCompletionSource.Task;
        }
        
        public static UniTask<TaskCompletionReason> RenewTask(ref UniTaskCompletionSource<TaskCompletionReason> taskCompletionSource, CancellationToken cancellationToken)
        {
            if (taskCompletionSource != null && !taskCompletionSource.Task.Status.IsCompleted())
            {
                taskCompletionSource.TrySetResult(TaskCompletionReason.Renew);
            }
            
            taskCompletionSource = new UniTaskCompletionSource<TaskCompletionReason>();
            
            if (cancellationToken.IsCancellationRequested)
            {
                taskCompletionSource.TrySetResult(TaskCompletionReason.CancellationToken);
            }
            else
            {
                var memTaskCompletionSource = taskCompletionSource;
                var registration = cancellationToken.Register(() =>
                {
                    memTaskCompletionSource.TrySetResult(TaskCompletionReason.CancellationToken);
                });
                taskCompletionSource.Task.GetAwaiter().OnCompleted(() =>
                {
                    registration.Dispose();
                });
            }

            return taskCompletionSource.Task;
        }

        
        /// <summary>
        /// Forget about task
        /// </summary>
        /// <param name="task"></param>
        /// <param name="acceptableExceptions"></param>
        public static async void Forget(this Task task, params Type[] acceptableExceptions)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (acceptableExceptions == null || acceptableExceptions.Length == 0)
                {
                    if (ex is OperationCanceledException)
                        return;
                    throw;
                }
                else
                {
                    // TODO: consider whether derived types are also acceptable.
                    if (!((IList) acceptableExceptions).Contains(ex.GetType()))
                        throw;
                }
            }
        }
        
        /// <summary>
        /// Forget about task except non-cancellation exceptions
        /// </summary>
        /// <param name="task"></param>
        public static async Task ForgetCancellation(this Task task)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // ReSharper disable once UseNegatedPatternInIsExpression
                if (!(ex is OperationCanceledException))
                    throw;
            }
        }

        public readonly struct SimpleCancellationTokenAwaiter: ICriticalNotifyCompletion
        {
            public readonly CancellationToken token;
            public SimpleCancellationTokenAwaiter(CancellationToken token)
            {
                this.token = token;
            }

            public bool IsCompleted => this.token.IsCancellationRequested;

            public void GetResult() { }

            public void OnCompleted(Action continuation)
            {
                if (this.token.IsCancellationRequested)
                {
                    continuation();
                }
                else
                {
                    this.token.Register(continuation);
                }
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                if (this.token.IsCancellationRequested)
                {
                    continuation();
                }
                else
                {
                    this.token.Register(continuation);
                }
            }
            
            public SimpleCancellationTokenAwaiter GetAwaiter()
            {
                return this;
            }
        }

        public static SimpleCancellationTokenAwaiter WhenCancellationToken(CancellationToken token)
        {
            return new SimpleCancellationTokenAwaiter(token);
        }

        public static SimpleCancellationTokenAwaiter WhenCancelled(this CancellationToken token)
        {
            return new SimpleCancellationTokenAwaiter(token);
        }
        
        public static IEnumerator EnumeratorAwaitable(Task task)
        {
            while (!task.IsCompleted) { yield return null; }
            if (task.IsFaulted) { throw task.Exception ?? new Exception("Task failed, but has no exception"); }
        }
        
        public static UniTask CustomAwaitable(Func<Action, Action> addHandler, CancellationToken cancellationToken = default)
        {
            var tcs = new UniTaskCompletionSource();
            if (cancellationToken.IsCancellationRequested)
            {
                tcs.TrySetResult();
                return tcs.Task;
            }
            Action complete = () =>
            {
                tcs.TrySetResult();
            };
            Action release = addHandler(complete);
            
            var registration = cancellationToken.Register(() =>
            {
                tcs.TrySetResult();
            });
            tcs.Task.GetAwaiter().OnCompleted(() =>
            {
                release();
                registration.Dispose();
            });
            return tcs.Task;
        }
        
        /// <summary>
        /// Make cancellation token awaitable
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static UniTask AsUniTask(this CancellationToken cancellationToken)
        {
            var completionSource = AutoResetUniTaskCompletionSource.Create();
            CancellationTokenRegistration registration = cancellationToken.Register(() =>
            {
                completionSource.TrySetResult();
            });

            return completionSource.Task.ContinueWith(() => registration.Dispose());
        }
        
        public static void ExecuteDelayed(Action action, float delay = 0f, CancellationToken cancellationToken = default)
        {
            if (delay > 0f)
            {
                UniTask.Delay(TimeSpan.FromSeconds(delay), true, PlayerLoopTiming.TimeUpdate, cancellationToken).ContinueWith(action).Forget();
            }
            else
            {
                action();
            }
        }
    }
}