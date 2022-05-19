using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Effects
{
    public class EffectExecutionContext: IDisposable
    {
        public enum Status
        {
            Idle,
            Pending,
            Completed
        }

        public string debugName = "";
        public readonly IEffect effect;
        public readonly CancellationTokenSource cancellationTokenSource;
        public Status status { get; protected set; } = Status.Pending;
        public event Action OnRelease;
        public UniTask task;
        public PluralAwaitable<bool> cycle = new PluralAwaitable<bool>(true);

        public EffectExecutionContext(IEffect effect)
        {
            this.effect = effect;
            this.cancellationTokenSource = new CancellationTokenSource();
            cycle.context = this;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Replay()
        {
            cycle.SetResult(true);
        }

        public void Dispose()
        {
            cycle.SetResult(false);
            cancellationTokenSource?.Cancel();
        }

        protected void Release()
        {
            status = Status.Completed;
            OnRelease?.Invoke();
        }
        
        public EffectCancellableOperationAwaiter GetAwaiter()
        {
            return new EffectCancellableOperationAwaiter(this);
        }

        public override string ToString()
        {
            return $"{effect.GetEffectName()}{(debugName?.Length > 0 ? $" ({debugName})" : "")}";
        }
    }
    public class EffectExecutionContext<TTarget>: EffectExecutionContext
        where TTarget: Behaviour
    {
        public readonly EffectsChannel<TTarget> channel;
        public UniTask<bool> replayUniTask { get; protected set; }

        

        public TTarget target
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => channel.target;
        }

        public EffectExecutionContext(EffectsChannel<TTarget> channel, IEffect effect): base(effect)
        {
            this.channel = channel;
            
        }
        
        public new void Release()
        {
            channel.executions.Remove(this);
            
            #if UNITY_EDITOR
            channel.editor_EmitExecutionsChange();
            #endif
            
            base.Release();
        }
    }
    public class EffectExecutionContext<TTarget, TEffectProps>: EffectExecutionContext<TTarget>
        where TTarget: Behaviour
    {
        public TEffectProps props;

        public EffectExecutionContext(EffectsChannel<TTarget> channel, IEffectWithProps<TTarget, TEffectProps> effect, ref TEffectProps props): base(channel, effect)
        {
            this.props = props;
            channel.executions.Add(this);
            
            #if UNITY_EDITOR
            channel.editor_EmitExecutionsChange();
            #endif
            
            task = effect.RunEffect(this, this.cancellationTokenSource.Token);
            
            if (task.Status != UniTaskStatus.Pending)
            {
                Release();
                return;
            }

            AsyncUtils.AwaitCallback(task, Release);
        }
        
        public EffectExecutionContext(EffectsChannel<TTarget> channel, IEffect<TTarget> effect): base(channel, effect)
        {
            channel.executions.Add(this);
            
            #if UNITY_EDITOR
            channel.editor_EmitExecutionsChange();
            #endif
            
            task = effect.RunEffect(this, this.cancellationTokenSource.Token);
            
            if (task.Status != UniTaskStatus.Pending)
            {
                Release();
                return;
            }

            AsyncUtils.AwaitCallback(task, Release);
        }
    }
}