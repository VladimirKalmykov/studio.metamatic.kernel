using System;
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
        
        public static DisposableAction PlayerLoopAction(Action callback, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default)
        {
            var active = true;
            UniTask.WaitUntil(() =>
            {
                if (!active) return true;
                callback();
                
                return !active;

            }, timing, cancellationToken).Forget();
            
            return new DisposableAction(() =>
            {
                active = false;
            });
        }
        
        public static DisposableAction PlayerLoopActionFps(Action<float> callback, PlayerLoopTiming timing = PlayerLoopTiming.Update, float fps = 60, CancellationToken cancellationToken = default)
        {
            /* Reduce fps to specified value */
            var interval = 1f / fps;
            var lastTime = Time.time;
            var active = true;
            callback(0f);
            UniTask.WaitUntil(() =>
            {
                if (!active) return true;
                var currentTime = Time.time;
                if (currentTime - lastTime >= interval)
                {
                    callback(currentTime - lastTime);
                    lastTime = currentTime;
                }
                
                return !active;

            }, timing, cancellationToken).Forget();
            
            return new DisposableAction(() =>
            {
                active = false;
            });
        }
        
        public static async UniTask PlayerLoopActionFps(Func<float, bool> callback, PlayerLoopTiming timing = PlayerLoopTiming.Update, float fps = 60, CancellationToken cancellationToken = default)
        {
            /* Reduce fps to specified value */
            var interval = 1f / fps;
            var lastTime = Time.time;
            await UniTask.WaitUntil(() =>
            {
                var currentTime = Time.time;
                var ready = false;
                if (currentTime - lastTime >= interval)
                {
                    ready =callback(currentTime - lastTime);
                    lastTime = currentTime;
                }
                
                return ready;

            }, timing, cancellationToken);
        }
        
        public static UniTask PlayerLoopAction(Action<float> callback, PlayerLoopTiming timing = PlayerLoopTiming.Update, float duration = 0f, CancellationToken cancellationToken = default)
        {
            var startTime = Time.time;
            callback(0f);
            return UniTask.WaitUntil(() =>
            {
                var ratio = duration > 0f ? Mathf.Clamp01((Time.time - startTime) / duration) : 1f;
                if (ratio >= 1f)
                {
                    callback(1f);
                    return true;
                }
                
                try {
                    callback(ratio);
                } catch (Exception e) {
                    Debug.LogException(e);
                    return true;
                }
                
                return false;

            }, timing, cancellationToken);
        }
        
        public static UniTask PlayerLoopActionSignal(Action<float> callback, PlayerLoopTiming timing = PlayerLoopTiming.Update, float duration = 0f, Signal cancellationSignal = default)
        {
            var startTime = Time.time;
            callback(0f);
            if (cancellationSignal.IsCancellationRequested)
            {
                return UniTask.CompletedTask;
            }
            return UniTask.WaitUntil(() =>
            {
                if (cancellationSignal.IsCancellationRequested)
                {
                    return true;
                }
                var ratio = duration > 0f ? Mathf.Clamp01((Time.time - startTime) / duration) : 1f;
                if (ratio >= 1f)
                {
                    callback(1f);
                    return true;
                }
                
                try {
                    callback(ratio);
                } catch (Exception e) {
                    Debug.LogException(e);
                    return true;
                }
                
                return false;

            }, timing);
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
                tokenSource.Dispose();
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
                tokenSource.Dispose();
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
        
        public static async UniTask RunOnThreadPool(Action action, bool configureAwait = true, Signal cancellationSignal = default)
        {
            cancellationSignal.ThrowIfRaised();

            await UniTask.SwitchToThreadPool();
            if (configureAwait)
            {
                try
                {
                    cancellationSignal.ThrowIfRaised();
                    action();
                }
                finally
                {
                    await UniTask.Yield();
                }
            }
            else
            {
                cancellationSignal.ThrowIfRaised();
                action();
            }

            cancellationSignal.ThrowIfRaised();
        }
    }
}