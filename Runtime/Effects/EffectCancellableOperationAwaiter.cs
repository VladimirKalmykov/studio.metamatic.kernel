using System;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Effects
{
    public struct EffectCancellableOperationAwaiter: ICriticalNotifyCompletion, IDisposable
    {
        public readonly EffectExecutionContext context;
        public EffectCancellableOperationAwaiter(EffectExecutionContext context)
        {
            this.context = context;
        }
        
        #region Notify completion
        
        // ReSharper disable once MergeIntoNegatedPattern
        public bool IsCompleted => (context == null || context.status != EffectExecutionContext.Status.Pending);

        public void GetResult() { }

        public void OnCompleted(Action continuation)
        {
            // ReSharper disable once MergeIntoNegatedPattern
            if (context == null || context.status != EffectExecutionContext.Status.Pending)
            {
                continuation();
                return;
            }

            context.OnRelease += continuation;
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            // ReSharper disable once MergeIntoNegatedPattern
            if (context == null || context.status != EffectExecutionContext.Status.Pending)
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