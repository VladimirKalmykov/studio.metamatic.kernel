using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
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
        
        /// <summary>
        /// Accept cancellationTokenSource and immediatelly cancel it in order to create new one. New token will be passed to callback function.
        /// </summary>
        /// <param name="tokenSource"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static Task Singular(ref CancellationTokenSource tokenSource, Func<CancellationToken, Task> callback)
        {
            if (tokenSource != null && !tokenSource.IsCancellationRequested)
            {
                tokenSource.Cancel();
            }

            tokenSource = new CancellationTokenSource();
            return callback(tokenSource.Token);
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
                // TODO: consider whether derived types are also acceptable.
                if (!((IList) acceptableExceptions).Contains(ex.GetType()))
                    throw;
            }
        }
        
        /// <summary>
        /// Forget about task except non-cancellation exceptions
        /// </summary>
        /// <param name="task"></param>
        public static async void ForgetCancellation(this Task task)
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
    }
}