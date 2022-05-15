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
    public class Effector : MonoBehaviour
    {
        public readonly List<EffectOperationContext> effectExecutions = new List<EffectOperationContext>();

        private void Start()
        {
            UILog.Add("Effector", "playbacks.Count", () => effectExecutions.Count);
        }
        
        private void OnDestroy()
        {
            foreach (var playback in effectExecutions)
            {
                playback.Dispose();
            }
        }
    }
    
    public class Effector<TTarget> : Effector
        where TTarget: MonoBehaviour
    {
        public TTarget target { get; private set; }

        public EffectCancellableOperationAwaiter ApplyEffect<TEffectProps>(IEffectWithProps<TTarget, TEffectProps> effect, TEffectProps props = default)
        {
            return new EffectExecutionContext<TTarget, TEffectProps>(
                this,
                effect,
                ref props
            ).GetAwaiter();
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