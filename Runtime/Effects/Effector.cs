using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Metamatic;
using Metamatic.DevTools;

namespace Metamatic.Effects
{
    public class EffectPlayback: ICriticalNotifyCompletion, IDisposable
    {
        public readonly Effector effector;
        public readonly CancellationTokenSource cancellationTokenSource;

        public EffectPlayback(Effector effector, CancellationTokenSource cancellationTokenSource)
        {
            this.effector = effector;
            this.cancellationTokenSource = cancellationTokenSource;
            effector.playbacks.Add(this);
            OnCompleted(Release);
        }

        public void Release()
        {
            effector.playbacks.Remove(this);
        }

        public void Dispose()
        {
            this.cancellationTokenSource.Cancel();
        }
        
        #region Notify completion
        
        public bool IsCompleted => this.cancellationTokenSource.IsCancellationRequested;

        public void GetResult() { }

        public void OnCompleted(Action continuation)
        {
            if (this.cancellationTokenSource.IsCancellationRequested)
            {
                continuation();
                return;
            }

            cancellationTokenSource.Token.Register(continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            if (this.cancellationTokenSource.IsCancellationRequested)
            {
                continuation();
                return;
            }

            cancellationTokenSource.Token.Register(continuation);
        }
            
        public EffectPlayback GetAwaiter()
        {
            return this;
        }

        #endregion
    }

    public class Effector : MonoBehaviour
    {
        public readonly List<EffectPlayback> playbacks = new List<EffectPlayback>();

        private void Awake()
        {
            playbacks.Capacity = 10000;
            
        }

        private void Start()
        {
            UILog.Add("Effector", "playbacks.Count", () => playbacks.Count);
        }
        
        private void OnDestroy()
        {
            foreach (var playback in playbacks)
            {
                playback.Dispose();
            }
        }
    }
    
    public class Effector<TTarget> : Effector
        where TTarget: MonoBehaviour
    {
        public TTarget target { get; private set; }
        
        public EffectPlayback ApplyEffect(IEffect<TTarget> effect)
        {
            return ApplyEffect(effect as IEffectWithProps<TTarget, object>);
        }

        public EffectPlayback ApplyEffect<TEffectProps>(IEffectWithProps<TTarget, TEffectProps> effect, ref TEffectProps props)
        {
            // var playback = new EffectPlaybackContext<TTarget>(this, effect);
            // playbacks.Add(playback);
            var cancellationTokenSource = new CancellationTokenSource();
            effect.RunEffect(this, props, cancellationTokenSource);
            return new EffectPlayback(this, cancellationTokenSource);

            // return playback;
        }

        public void ReleaseEffect(IEffect<TTarget> effect)
        {
            
        }

        private void Awake()
        {
            target = GetComponent<TTarget>();
        }

        
    }
}