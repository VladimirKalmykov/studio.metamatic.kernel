using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Metamatic.Effects
{
    public abstract class ScriptableEffect: ScriptableObject, IEffect
    {
        public abstract string GetEffectName();
        
        public EffectCancellableOperationAwaiter Apply(Behaviour behaviour, object props = default)
        {
            // var effector = behaviour.GetComponent<TTarget>() as Effector<TTarget>;
            // return effector.ApplyEffect(this, props);
            return ApplyWithProps(behaviour, props);
        }
        internal abstract EffectCancellableOperationAwaiter ApplyWithProps(Behaviour behaviour, object props = default);
    }
    public abstract class ScriptableEffect<TTarget> : ScriptableEffect, IEffect<TTarget>
        where TTarget: Behaviour
    {
        /// <summary>
        /// Determines the behaviour of the effect when another same effect applies
        /// </summary>
        public abstract EffectDuplicationBehaviour effectDuplication { get; }
        
        // [Obsolete("Dont' use runEffect with TTarget. Use effect.Apply instead. The reason is passing Effects propsless")]
        // public abstract UniTask RunEffect(EffectExecutionContext<TTarget> executionContext,
        //     CancellationToken cancellationToken);
    }
    
    public abstract class ScriptableEffect<TEffector, TProps> : ScriptableEffect<TEffector>, IEffectWithProps<TEffector, TProps>
        where TEffector: Effector
        where TProps: struct
    {
        public UniTask InternalRunEffect(EffectExecutionContext<TEffector, TProps> executionContext,
            CancellationToken cancellationToken)
        {
            return WrapEffectTask(RunEffect(executionContext, cancellationToken), executionContext, cancellationToken);
        }
        public abstract UniTask RunEffect(EffectExecutionContext<TEffector, TProps> executionContext,
            CancellationToken cancellationToken);
        
        /// <summary>
        /// Allows to wrap effect task to do smth before after effect execution
        /// </summary>
        /// <param name="effectTask"></param>
        /// <returns></returns>
        public virtual UniTask WrapEffectTask(UniTask effectTask, EffectExecutionContext<TEffector, TProps> executionContext,
            CancellationToken cancellationToken)
        {
            return effectTask;
        }
        
        public EffectCancellableOperationAwaiter Apply(Behaviour behaviour, TProps props = default)
        {
            var effector = behaviour.GetComponent<TEffector>();
            if (effector == null)
            {
                throw new Exception($"Target has no opporiate effector with type {typeof(TEffector).Name}");
            }

            return Apply(effector, ref props);
        }
        
        /// <summary>
        /// Method that apply effect to the effector and it can be customized in the derived class
        /// </summary>
        /// <param name="effector"></param>
        /// <param name="props"></param>
        /// <returns></returns>
        protected virtual EffectCancellableOperationAwaiter Apply(TEffector effector, ref TProps props)
        {
            return effector.ApplyEffect(this, props);
        }
        /// <summary>
        /// Method used only internal
        /// </summary>
        /// <param name="behaviour"></param>
        /// <param name="props"></param>
        /// <returns></returns>
        internal sealed override EffectCancellableOperationAwaiter ApplyWithProps(Behaviour behaviour, object props = default)
        {
            return Apply(behaviour, props == null ? default(TProps) : (TProps)props);
        }
    }

    /// <summary>
    /// GameObject only asset reference.
    /// </summary>
    [System.Serializable]
    public class AssetReferenceScriptableEffect : AssetReferenceT<ScriptableEffect>
    {
        /// <summary>
        /// Constructs a new reference to a GameObject.
        /// </summary>
        /// <param name="guid">The object guid.</param>
        public AssetReferenceScriptableEffect(string guid) : base(guid) {}
    }
    
    /// <summary>
    /// GameObject only asset reference.
    /// </summary>
    [System.Serializable]
    public class AssetReferenceScriptableEffect<TEffector, TState> : AssetReferenceT<ScriptableEffect<TEffector, TState>>
    where TEffector : Effector
    where TState : struct
    {
        /// <summary>
        /// Constructs a new reference to a GameObject.
        /// </summary>
        /// <param name="guid">The object guid.</param>
        public AssetReferenceScriptableEffect(string guid) : base(guid) {}
    }
}