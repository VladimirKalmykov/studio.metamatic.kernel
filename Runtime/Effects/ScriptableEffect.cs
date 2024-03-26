using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Metamatic.Effects
{
    public struct NoEffectProps
    {
        
    }
    public abstract class ScriptableEffect: ScriptableObject, IEffect
    {
        public abstract string GetEffectName();
        
        public EffectCancellableOperationAwaiter Apply(Behaviour behaviour, object props = default)
        {
            // var effector = behaviour.GetComponent<TTarget>() as Effector<TTarget>;
            // return effector.ApplyEffect(this, props);
            return ApplyWithProps(behaviour, props);
        }
        public EffectCancellableOperationAwaiter Apply(GameObject go, object props = default)
        {
            // var effector = behaviour.GetComponent<TTarget>() as Effector<TTarget>;
            // return effector.ApplyEffect(this, props);
            return ApplyWithProps(go, props);
        }
        public abstract EffectDuplicationBehaviour effectDuplication { get; }
        protected abstract EffectCancellableOperationAwaiter ApplyWithProps(Behaviour behaviour, object props = default);
        protected abstract EffectCancellableOperationAwaiter ApplyWithProps(GameObject go, object props = default);
    }
    
    public abstract class ScriptableEffect<TEffector, TProps, TResult> : ScriptableEffect, IEffect<TEffector, TProps, TResult>
        where TEffector: Effector
        where TProps: struct
    {
        public UniTask InternalRunEffect(IEffect<TEffector, TProps, TResult>.EffectExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            return WrapEffectTask(RunEffect(executionContext, cancellationToken), executionContext, cancellationToken);
        }
        public abstract UniTask RunEffect(IEffect<TEffector, TProps, TResult>.EffectExecutionContext executionContext,
            CancellationToken cancellationToken);
        
        /// <summary>
        /// Allows to wrap effect task to do smth before after effect execution
        /// </summary>
        /// <param name="effectTask"></param>
        /// <returns></returns>
        public virtual UniTask WrapEffectTask(UniTask effectTask, IEffect<TEffector, TProps, TResult>.EffectExecutionContext executionContext,
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
        
        public EffectCancellableOperationAwaiter Apply(GameObject go, TProps props = default)
        {
            var effector = go.GetComponent<TEffector>();
            if (effector == null)
            {
                throw new Exception($"Target has no appropriate effector with type {typeof(TEffector).Name}");
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
        protected sealed override EffectCancellableOperationAwaiter ApplyWithProps(Behaviour behaviour, object props = default)
        {
            return Apply(behaviour, props == null ? default(TProps) : (TProps)props);
        }

        protected sealed override EffectCancellableOperationAwaiter ApplyWithProps(GameObject go, object props = default)
        {
            return Apply(go, props == null ? default(TProps) : (TProps)props);
        }
    }
    
    public abstract class ScriptableEffect<TEffector, TProps> : ScriptableEffect<TEffector, TProps, object>
        where TEffector: Effector
        where TProps: struct
    {
        
    }
    
    public abstract class ScriptableEffect<TEffector> : ScriptableEffect<TEffector, NoEffectProps, object>
        where TEffector: Effector
    {
        
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
    public class AssetReferenceScriptableEffect<TEffector, TState, TResult> : AssetReferenceT<ScriptableEffect<TEffector, TState, TResult>>
    where TEffector : Effector
    where TState : struct
    {
        /// <summary>
        /// Constructs a new reference to a GameObject.
        /// </summary>
        /// <param name="guid">The object guid.</param>
        public AssetReferenceScriptableEffect(string guid) : base(guid) {}
    }
    
    [System.Serializable]
    public class AssetReferenceScriptableEffect<TEffector, TProps> : AssetReferenceT<ScriptableEffect<TEffector, TProps>>
        where TEffector : Effector
        where TProps : struct
    {
        /// <summary>
        /// Constructs a new reference to a GameObject.
        /// </summary>
        /// <param name="guid">The object guid.</param>
        public AssetReferenceScriptableEffect(string guid) : base(guid) {}
    }
}