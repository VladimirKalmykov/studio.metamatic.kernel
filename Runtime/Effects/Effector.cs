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
        public readonly List<EffectsChannel> channels = new List<EffectsChannel>();
        
        
        #if UNITY_EDITOR
        public event Action<EffectsChannel[]> editor_OnChannelsChange;
        public void editor_SubscribeChannels(Action<EffectsChannel[]> handler)
        {
            handler(channels.ToArray());
            editor_OnChannelsChange += handler;
        }
        
        public void editor_UnsubscribeChannels(Action<EffectsChannel[]> handler)
        {
            editor_OnChannelsChange -= handler;
        }

        protected void editor_EmitChannelsChange()
        {
            editor_OnChannelsChange?.Invoke(channels.ToArray());
        }
        #endif

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
        where TTarget: Behaviour
    {
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
            var channel = new EffectsChannel<TTarget>(this, owner, debugName);
            channels.Add(channel);
            #if UNITY_EDITOR
            editor_EmitChannelsChange();
            #endif
            return channel;
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