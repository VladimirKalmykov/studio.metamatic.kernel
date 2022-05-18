using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Metamatic.Effects.Graph;
using UnityEngine;

namespace Metamatic.Effects
{
    public class EffectsChannel
    {
        /// <summary>
        /// Effects execution context queue
        /// </summary>
        public readonly List<EffectExecutionContext> executions = new List<EffectExecutionContext>();
        public readonly Behaviour owner;
        public readonly string debugName;
        
        #if UNITY_EDITOR
        public event Action<EffectExecutionContext[]> editor_OnExecutionsChange;
        public void editor_SubscribeEffectExecutionsChange(Action<EffectExecutionContext[]> handler)
        {
            handler(executions.ToArray());
            editor_OnExecutionsChange += handler;
        }
        
        public void editor_UnsubscribeEffectExecutionsChange(Action<EffectExecutionContext[]> handler)
        {
            editor_OnExecutionsChange -= handler;  
        }

        public void editor_EmitExecutionsChange()
        {
            editor_OnExecutionsChange?.Invoke(executions.ToArray());
        }
        #endif

        public EffectsChannel(Behaviour owner, string debugName)
        {
            this.owner = owner;
            this.debugName = debugName;
        }

        public override string ToString()
        {
            return $"{owner.name}: {debugName}";
        }
    }
    public sealed class EffectsChannel<TTarget>: EffectsChannel, IDisposable
        where TTarget: MonoBehaviour
    {
        public readonly EffectsGraph<TTarget> graph; 
        public readonly Effector<TTarget> effector;

        public TTarget target
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return effector.target;
            }
        }
        public EffectsChannel(Effector<TTarget> effector, Behaviour owner, string debugName): base(owner, debugName)
        {
            this.graph = new EffectsGraph<TTarget>(this);
            this.effector = effector;
            effector.OnBeforeDestroy += OnDestroy;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnDestroy()
        {
            CancelAll();
        }
        
        public EffectCancellableOperationAwaiter Add(IEffect<TTarget> effect)
        {
            return new EffectExecutionContext<TTarget, object>(
                this,
                effect
            ).GetAwaiter();
        }

        public EffectCancellableOperationAwaiter Add<TEffectProps>(IEffectWithProps<TTarget, TEffectProps> effect, TEffectProps props = default)
        {
            return new EffectExecutionContext<TTarget, TEffectProps>(
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