using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Effects
{
    public enum EffectDuplicationBehaviour
    {
        Overlay,
        Recreate,
        Update
    }
    public interface IEffectLegacy
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
            public readonly IEffectLegacy effect;
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
                cancellationTokenSource?.Dispose();
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
            
            

            public EffectExecutionContext(EffectsChannel channel, IEffectLegacy effect)
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
        }
        public string GetEffectName();
        public EffectDuplicationBehaviour effectDuplication { get; } 
    }
    public interface IEffect<TTarget>: IEffectLegacy
        where TTarget: Behaviour
    {
        
    }

    public interface IEffect<TEffector, TProps, TResult> : IEffect<TEffector>
        where TEffector: EffectorLegacy
        where TProps: struct
    {
        
       
        public class EffectExecutionContext: IEffectLegacy.EffectExecutionContext
        {
            private TResult m_Result;
            public TProps props;
            
            public bool m_PropsUpdated;
            
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

            public delegate void PropsModifier(ref TProps props);

            public EffectExecutionContext(EffectsChannel channel, IEffect<TEffector, TProps, TResult> effect, ref TProps props): base(channel, effect)
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
            public void UpdateProps(TProps nextProps)
            {
                this.props = nextProps;
                this.m_PropsUpdated = true;
                this.Replay();
            }
            
            public void SetResult(TResult result)
            {
                m_Result = result;
            }
            
            public TResult GetResult()
            {
                return m_Result;
            }
        }
        public UniTask RunEffect(EffectExecutionContext executionContext, CancellationToken cancellationToken);

        public UniTask InternalRunEffect(EffectExecutionContext executionContext,
            CancellationToken cancellationToken);
    }
    
    public interface IEffect<TEffector, TProps> : IEffect<TEffector, TProps, object>
        where TEffector: EffectorLegacy
        where TProps: struct
    {
        public new UniTask RunEffect(EffectExecutionContext executionContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public new UniTask InternalRunEffect(EffectExecutionContext executionContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}