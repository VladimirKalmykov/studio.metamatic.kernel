using System;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Effects
{
    public readonly struct EffectCancellableOperationAwaiter: ICriticalNotifyCompletion, IDisposable
    {
        public readonly EffectOperationContext context;
        public EffectCancellableOperationAwaiter(EffectOperationContext context)
        {
            this.context = context;
        }
        
        #region Notify completion
        
        public bool IsCompleted => context.task.Status != UniTaskStatus.Pending;

        public void GetResult() { }

        public void OnCompleted(Action continuation)
        {
            if (context.task.Status != UniTaskStatus.Pending)
            {
                continuation();
                return;
            }

            AsyncUtils.AwaitCallback(context.task, continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            if (context.task.Status != UniTaskStatus.Pending)
            {
                continuation();
                return;
            }

            AsyncUtils.AwaitCallback(context.task, continuation);
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