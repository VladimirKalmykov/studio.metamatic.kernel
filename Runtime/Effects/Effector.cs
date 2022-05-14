using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Metamatic;

namespace Metamatic.Effects
{
    public readonly struct CancellableEffectPlayback: ICriticalNotifyCompletion
    {
        public readonly CancellationTokenSource cancellationTokenSource;
        public readonly Task task;

        public CancellableEffectPlayback(CancellationTokenSource cancellationTokenSource, Task task)
        {
            this.cancellationTokenSource = cancellationTokenSource;
            this.task = task;
        }

        public void Cancel()
        {
            this.cancellationTokenSource.Cancel();
        }
        
        #region Notify completion
        
        public bool IsCompleted => this.task.IsCompleted;

        public void GetResult() { }

        public void OnCompleted(Action continuation)
        {
            if (this.task.IsCompleted)
            {
                continuation();
                return;
            }

            AsyncUtils.AwaitCallback(this.task, continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            if (this.task.IsCompleted)
            {
                continuation();
                return;
            }

            AsyncUtils.AwaitCallback(this.task, continuation);
        }
            
        public CancellableEffectPlayback GetAwaiter()
        {
            return this;
        }

        #endregion
    }

    public class Effector<TTarget> : MonoBehaviour
        where TTarget: MonoBehaviour
    {
        public TTarget target { get; private set; }

        
        
        public CancellableEffectPlayback ApplyEffect(IEffect<TTarget> effect)
        {
            return ApplyEffect(effect as IEffectWithProps<TTarget, object>);
        }

        public CancellableEffectPlayback ApplyEffect<TEffectProps>(IEffectWithProps<TTarget, TEffectProps> effect, TEffectProps props = default)
        {
            // var playback = new EffectPlaybackContext<TTarget>(this, effect);
            // playbacks.Add(playback);
            var cancellationTokenSource = new CancellationTokenSource();
            return new CancellableEffectPlayback(cancellationTokenSource, effect.RunEffect(this, props, cancellationTokenSource.Token));

            // return playback;
        }

        public void ReleaseEffect(IEffect<TTarget> effect)
        {
            
        }

        private void Awake()
        {
            target = GetComponent<TTarget>();
        }

        private void OnDestroy()
        {
            
        }
    }
}