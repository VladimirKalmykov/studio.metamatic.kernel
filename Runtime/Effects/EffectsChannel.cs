using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Metamatic.Effects.Graph;
using UnityEngine;

namespace Metamatic.Effects
{
    public class EffectsChannel: IDisposable
    {
        /// <summary>
        /// Effects execution context queue
        /// </summary>
        public readonly List<IEffect.EffectExecutionContext> executions = new List<IEffect.EffectExecutionContext>();
        public readonly Behaviour owner;
        public readonly string debugName;
        
        // private EffectsGraph m_DefaultGraph;
        //
        // public EffectsGraph graph
        // {
        //     get
        //     {
        //         return m_DefaultGraph == null ? (m_DefaultGraph = new EffectsGraph(this.m_CommonChannel)) : m_DefaultGraph;
        //     }
        // }
        
        #if UNITY_EDITOR
        public event Action<IEffect.EffectExecutionContext[]> editor_OnExecutionsChange;
        public void editor_SubscribeEffectExecutionsChange(Action<IEffect.EffectExecutionContext[]> handler)
        {
            handler(executions.ToArray());
            editor_OnExecutionsChange += handler;
        }
        
        public void editor_UnsubscribeEffectExecutionsChange(Action<IEffect.EffectExecutionContext[]> handler)
        {
            editor_OnExecutionsChange -= handler;  
        }

        public void editor_EmitExecutionsChange()
        {
            editor_OnExecutionsChange?.Invoke(executions.ToArray());
        }
        #endif

        public override string ToString()
        {
            return $"{owner.name}: {debugName}";
        }
        
        public readonly EffectsGraph graph; 
        public readonly Effector effector;

        public Effector target
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return effector.target;
            }
        }
        public EffectsChannel(Effector effector, Behaviour owner, string debugName)
        {
            this.owner = owner;
            this.debugName = debugName;
            this.graph = new EffectsGraph(this);
            this.effector = effector;
            effector.OnBeforeDestroy += OnDestroy;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnDestroy()
        {
            CancelAll();
        }
        
        /*
        [Obsolete("Add propsless effect to the channel is deprecated", true)]
        public EffectCancellableOperationAwaiter Add(IEffect<TTarget> effect)
        {
            switch (effect.effectDuplication)
            {
                case EffectDuplicationBehaviour.Overlay:
                    break;
                case EffectDuplicationBehaviour.Recreate:
                case EffectDuplicationBehaviour.Update:
                    var executingEffectExecutionContext = executions.Find(e => e.effect == effect);
                    executingEffectExecutionContext?.Dispose();

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return new EffectExecutionContext<TTarget, object>(
                this,
                effect
            ).GetAwaiter();
        }*/

        public EffectCancellableOperationAwaiter Add<TEffector, TEffectProps, TResult>(IEffect<TEffector, TEffectProps, TResult> effect, TEffectProps props = default) where TEffectProps: struct where TEffector: Effector
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                throw new Exception("You can not run effect in editor mode");
            }
            #endif
            switch (effect.effectDuplication)
            {
                case EffectDuplicationBehaviour.Overlay:
                    break;
                case EffectDuplicationBehaviour.Recreate:
                case EffectDuplicationBehaviour.Update:
                    var executingEffectExecutionContext = executions.Find(e => e.effect == effect);
                    if (executingEffectExecutionContext != null)
                    {
                        if (effect.effectDuplication == EffectDuplicationBehaviour.Recreate)
                        {
                            executingEffectExecutionContext.Dispose();
                        }
                        else
                        {
                            (executingEffectExecutionContext as IEffect<TEffector, TEffectProps, TResult>.EffectExecutionContext)
                                ?.UpdateProps(props);
                            return executingEffectExecutionContext.GetAwaiter();
                        }
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return new IEffect<TEffector, TEffectProps, TResult>.EffectExecutionContext(
                this,
                effect,
                ref props
            ).GetAwaiter();
        }

        /// <summary>
        /// Cancel every effect execution in reverse order (from latest to earliest)
        /// </summary>
        public void CancelAll()
        {
            var curerntExecutions = executions.ToArray();
            for (var i = curerntExecutions.Length - 1; i >= 0; i--)
            {
                executions[i].Dispose();
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            effector.OnBeforeDestroy -= OnDestroy;
            CancelAll();
        }
    }
}