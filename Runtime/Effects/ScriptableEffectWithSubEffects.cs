using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading;
using System.Linq;
using Cysharp.Threading.Tasks;
using Metamatic.Effects;

namespace Metamatic {
    using System;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using Sirenix.OdinInspector;
    using Metamatic.GameAccessManagment;
    
    public abstract class ScriptableEffectWithSubEffects<TEffector, TProps> : ScriptableEffect<TEffector, TProps>
        where TEffector: Effector
        where TProps : struct
    {
        [BoxGroup("Effect")]
        [SerializeField] private ScriptableEffect[] subEffects;
        
        public override async UniTask WrapEffectTask(UniTask effectTask, EffectExecutionContext<TEffector, TProps> executionContext, CancellationToken cancellationToken)
        {
            /* Apply effects */
            IEnumerable<EffectCancellableOperationAwaiter> subEffectExecutions = subEffects != null
                ? subEffects.Where(e => e != null).Select(
                    effect => effect.Apply(executionContext.target))
                : null;
            try
            {
                await effectTask;
            }
            finally
            {
                if (subEffectExecutions != null)
                {
                    foreach (var execution in subEffectExecutions)
                    {
                        execution.Dispose();
                    }
                }
            }
        }
    }
}