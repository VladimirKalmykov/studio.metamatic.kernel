using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Metamatic.Actions;
using Action = System.Action;

namespace Metamatic.Effects
{
    public abstract class EffectExecutionContext: IDisposable
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Replay()
        {
            cycle.SetResult(true);
        }

        public void Dispose()
        {
            cycle.SetResult(false);
            cancellationTokenSource?.Cancel();
        }
        
        public EffectCancellableOperationAwaiter GetAwaiter()
        {
            return new EffectCancellableOperationAwaiter(this);
        }

        public override string ToString()
        {
            return $"{effect.GetEffectName()}{(debugName?.Length > 0 ? $" ({debugName})" : "")}";
        }
        
        public readonly EffectsChannel channel;
        public UniTask<bool> replayUniTask { get; protected set; }
        
        

        public EffectExecutionContext(EffectsChannel channel, IEffect effect)
        {
            this.effect = effect;
            this.cancellationTokenSource = new CancellationTokenSource();
            cycle.context = this;
            this.channel = channel;
        }
        
        public new void Release()
        {
            channel.executions.Remove(this);
            
#if UNITY_EDITOR
            channel.editor_EmitExecutionsChange();
#endif
            
            status = Status.Completed;
            OnRelease?.Invoke();
        }

        public ApplyEffectActionLegacy GetAsAction()
        {
            return GetAsActionWithProps();
        }

        protected abstract ApplyEffectActionLegacy GetAsActionWithProps();
    }
   
    public class EffectExecutionContext<TEffector, TEffectProps>: EffectExecutionContext
        where TEffectProps: struct
        where TEffector: Effector
    {
        public TEffectProps props;
        
        public bool m_PropsUpdated = false;
        
        public TEffector target
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (TEffector)channel.target;
        }
        
        /// <summary>
        /// Returns true if props was updated. After getting value it turn back to false immediately.
        /// </summary>
        public bool propsUpdated
        {
            get
            {
                var updated = m_PropsUpdated;
                m_PropsUpdated = false;
                return updated;
            }
        }

        public delegate void PropsModifier(ref TEffectProps props);
        
        

        public EffectExecutionContext(EffectsChannel channel, IEffectWithProps<TEffector, TEffectProps> effect, ref TEffectProps props): base(channel, effect)
        {
            this.props = props;
            channel.executions.Add(this);
            
            #if UNITY_EDITOR
            channel.editor_EmitExecutionsChange();
            #endif
            
            task = effect.InternalRunEffect(this, this.cancellationTokenSource.Token);
            
            if (task.Status != UniTaskStatus.Pending)
            {
                if (task.Status == UniTaskStatus.Faulted)
                {
                    AsyncUtils.AwaitCallback(task, () =>
                    {
                        // Never
                    }, (Exception e) =>
                    {
                        throw e;
                    }, true);
                }
                Release();
                return;
            }

            AsyncUtils.AwaitCallback(task, () =>
            {
                /*channel
                    .effector
                    .dispatcher
                    .Dispatch(new EffectExitAction((ScriptableEffect)effect, this.props, new ActionSource("Effect Execution Control")));*/
                Release();
            });
        }
        
        /*[Obsolete("Using effect context with no props is deprecated", true)]
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
        }*/
        
        /// <summary>
        /// Eventless modify effect props
        /// </summary>
        /// <param name="modifier"></param>
        public void ModifyProps(PropsModifier modifier)
        {
            modifier(ref this.props);
        }
        
        /// <summary>
        /// Update effect props ant replay effect cycle
        /// </summary>
        /// <param name="nextProps"></param>
        public void UpdateProps(TEffectProps nextProps)
        {
            this.props = nextProps;
            this.m_PropsUpdated = true;
            this.Replay();
        }
        
        /// <summary>
        /// Generate an action that applies this effect
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Metamatic.Actions.ActionLegacy GenerateAction()
        {
            if (effect is not ScriptableEffect)
            {
                throw new Exception("Can not generate effect action for non scriptable effect");
            }
            return new ApplyEffectActionLegacy((ScriptableEffect)effect, props, new ActionSource("GenerateAction"));
        }

        protected override ApplyEffectActionLegacy GetAsActionWithProps()
        {
            if (effect is not ScriptableEffect)
            {
                throw new Exception("Can not generate effect action with non scriptable effect");
            }
            return new ApplyEffectActionLegacy(effect as ScriptableEffect, props, new ActionSource(target, "Recreated from execution"));
        }
    }
}