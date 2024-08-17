using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public class ReusableUniTaskCompletionSource : IUniTaskSource, ITaskPoolNode<ReusableUniTaskCompletionSource>, IPromise
    {
        static TaskPool<ReusableUniTaskCompletionSource> pool;
        ReusableUniTaskCompletionSource nextNode;
        public ref ReusableUniTaskCompletionSource NextNode => ref nextNode;

        static ReusableUniTaskCompletionSource()
        {
            TaskPool.RegisterSizeGetter(typeof(ReusableUniTaskCompletionSource), () => pool.Size);
        }

        UniTaskCompletionSourceCore<AsyncUnit> core;

        ReusableUniTaskCompletionSource()
        {
        }

        ~ReusableUniTaskCompletionSource()
        {
            /*#if DEBUG
            if (core.UnsafeGetStatus() == UniTaskStatus.Pending)
            {
                UnityEngine.Debug.LogError("You must call SetResult, SetException, or SetCanceled in ReusableUniTaskCompletionSource. Forget to call?");
            }
            #endif*/
        }

        public static ReusableUniTaskCompletionSource Create()
        {
            if (!pool.TryPop(out var result))
            {
                result = new ReusableUniTaskCompletionSource();
            }
            TaskTracker.TrackActiveTask(result, 2);
            return result;
        }

        [DebuggerHidden]
        public static ReusableUniTaskCompletionSource CreateFromCanceled(CancellationToken cancellationToken, out short token)
        {
            var source = Create();
            source.TrySetCanceled(cancellationToken);
            token = source.core.Version;
            return source;
        }

        [DebuggerHidden]
        public static ReusableUniTaskCompletionSource CreateFromException(Exception exception, out short token)
        {
            var source = Create();
            source.TrySetException(exception);
            token = source.core.Version;
            return source;
        }

        [DebuggerHidden]
        public static ReusableUniTaskCompletionSource CreateCompleted(out short token)
        {
            var source = Create();
            source.TrySetResult();
            token = source.core.Version;
            return source;
        }

        public UniTask Task
        {
            [DebuggerHidden]
            get
            {
                return new UniTask(this, core.Version);
            }
        }
        
        #if DEBUG
        public int debugVersion => core.Version;
        #endif

        [DebuggerHidden]
        public bool TrySetResult()
        {
            return core.TrySetResult(AsyncUnit.Default);
        }

        [DebuggerHidden]
        public bool TrySetCanceled(CancellationToken cancellationToken = default)
        {
            return core.TrySetCanceled(cancellationToken);
        }

        [DebuggerHidden]
        public bool TrySetException(Exception exception)
        {
            return core.TrySetException(exception);
        }

        #if DEBUG
        private bool m_Handled;
        #endif
        [DebuggerHidden]
        public void GetResult(short token)
        {
#if DEBUG
            m_Handled = true;
#endif
            core.GetResult(token);
        }

        public int version => core.Version;

        [DebuggerHidden]
        public UniTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        [DebuggerHidden]
        public UniTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }
        
        private List<(Action<object>, object)> continuations;
        
        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            /* Add support for multiple continuations (like in TaskCompletionSource, but keeping support core) */
            if (continuations == null || continuations.Count == 0)
            {
                if (continuations == null)
                {
                    continuations = new List<(Action<object>, object)>();
                }
                void Continuation(object s)
                {
                    /* Memoize the continuations, because after calling them this task completion source already can be reset or returned */
                    var memoizedContinuations = continuations.ToArray();
                    continuations = null;
                    foreach (var items in memoizedContinuations)
                    {
                        items.Item1(items.Item2);
                    }
                    
                }
                
                continuations.Add((continuation, state));
                core.OnCompleted(Continuation, state, token);
            }
            else
            {
                continuations.Add((continuation, state));
            }
        }
        
        private void InternalReset()
        {
#if DEBUG
            /*
             if (!m_Handled)
            {
                UnityEngine.Debug.LogError("You must call GetResult in ReusableUniTaskCompletionSource before Reset.");
            }
            */
            if (core.UnsafeGetStatus() == UniTaskStatus.Pending)
            {
                UnityEngine.Debug.LogError($"Task status is {core.UnsafeGetStatus()}. You must call SetResult, SetException, or SetCanceled in ReusableUniTaskCompletionSource before Reset.");
                TrySetException(new Exception("You must call SetResult, SetException, or SetCanceled in ReusableUniTaskCompletionSource before Reset."));
            }
#endif
            continuations?.Clear();
        }
        
        /// <summary>
        /// When task completion source is no longer used, you must call this method to return to pool.
        /// </summary>
        /// <returns></returns>
        public bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            InternalReset();
            core.Reset();
            return pool.TryPush(this);
        }
        
        /// <summary>
        /// When you want to reuse this task completion source right now, call this method. It resets the state and start tracking again.
        /// </summary>
        public void Reset()
        {
            TaskTracker.RemoveTracking(this);
            InternalReset();
            core.Reset();
            TaskTracker.TrackActiveTask(this, 2);
        }
    }
}