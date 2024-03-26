using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Metamatic.Effects
{
    public struct EffectCancellableOperationAwaiter: ICriticalNotifyCompletion, IDisposable
    {
        public readonly IEffect.EffectExecutionContext context;
        public EffectCancellableOperationAwaiter(IEffect.EffectExecutionContext context)
        {
            this.context = context;
        }

        public IEffect.EffectExecutionContext.Status status
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return context == null ? IEffect.EffectExecutionContext.Status.Idle : context.status;
            }
        }
        
        #region Notify completion
        
        // ReSharper disable once MergeIntoNegatedPattern
        public bool IsCompleted
        {
            get
            {
                if (context == null)
                {
                    Debug.Log($"Context is null. Why???");
                }
                var result = (context == null || context.status != IEffect.EffectExecutionContext.Status.Pending);

                return result;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Replay()
        {
            context.Replay();
        }

        public void GetResult() { }

        public void OnCompleted(Action continuation)
        {
            // ReSharper disable once MergeIntoNegatedPattern
            if (context == null || context.status != IEffect.EffectExecutionContext.Status.Pending)
            {
                continuation();
                return;
            }

            context.OnRelease += continuation;
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            // ReSharper disable once MergeIntoNegatedPattern
            if (context == null || context.status != IEffect.EffectExecutionContext.Status.Pending)
            {
                continuation();
                return;
            }

            context.OnRelease += continuation;
        }
            
        public EffectCancellableOperationAwaiter GetAwaiter()
        {
            return this;
        }

        #endregion
        
        public void Dispose()
        {
            context?.Dispose();
        }
    }
}