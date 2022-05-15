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
        public event Action OnBeforeDestroy;
        public readonly List<EffectExecutionContext> effectExecutions = new List<EffectExecutionContext>();

        private void Start()
        {
            UILog.Add("Effector", "playbacks.Count", () => effectExecutions.Count);
        }
        
        private void OnDestroy()
        {
            OnBeforeDestroy?.Invoke();
        }
    }
    
    public class Effector<TTarget> : Effector
        where TTarget: MonoBehaviour
    {
        public readonly List<EffectsChannel<TTarget>> channels = new List<EffectsChannel<TTarget>>();
        private EffectsChannel<TTarget> m_CommonChannel;
        public TTarget target { get; private set; }

        private void Awake()
        {
            target = GetComponent<TTarget>();
            
            /* Define common channel */
            m_CommonChannel = new EffectsChannel<TTarget>(this, this, "Common");
        }

        public EffectsChannel<TTarget> CreateChannel(Behaviour owner, string debugName)
        {
            return new EffectsChannel<TTarget>(this, owner, debugName);
        }

        public EffectCancellableOperationAwaiter ApplyEffect(IEffect<TTarget> effect)
        {
            return m_CommonChannel.Add(effect);
        }

        public EffectCancellableOperationAwaiter ApplyEffect<TEffectProps>(IEffectWithProps<TTarget, TEffectProps> effect, TEffectProps props = default)
        {
            return m_CommonChannel.Add(effect, props);
        }
    }
}