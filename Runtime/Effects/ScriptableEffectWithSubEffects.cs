using System.Threading;
using Cysharp.Threading.Tasks;
using Metamatic.Effects;

namespace Metamatic {
    using System;
    using UnityEngine;
    using Sirenix.OdinInspector;

    public abstract class ScriptableEffectWithSubEffects<TEffector, TProps> : ScriptableEffect<TEffector, TProps>
        where TEffector: Effector
        where TProps : struct
    {
        [BoxGroup("Effect")]
        [SerializeField] private ScriptableEffect[] subEffects;
        
        public override async UniTask WrapEffectTask(UniTask effectTask, EffectExecutionContext<TEffector, TProps> executionContext, CancellationToken cancellationToken)
        {
            Action disposer = null;
            /* Apply effects */
            if (subEffects != null) foreach (var effect in subEffects)
            {
                if (effect != null)
                {
                    var execution = effect.Apply(executionContext.target);
                    disposer += () => execution.Dispose();
                }
            }
            try
            {
                await effectTask;
            }
            finally
            {
                disposer?.Invoke();
            }
        }
    }
}