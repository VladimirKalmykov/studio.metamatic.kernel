using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Metamatic;
using Metamatic.Actions;
using Metamatic.DevTools;
using Action = System.Action;

namespace Metamatic.Effects
{   
    [RequireComponent(typeof(Dispatcher))]
    public class Effector : MonoBehaviour
    {
        public event Action OnBeforeDestroy;
        public readonly List<EffectExecutionContext> effectExecutions = new List<EffectExecutionContext>();
        public readonly List<EffectsChannel> channels = new List<EffectsChannel>();
        private Dispatcher m_Dispatcher;

        public Dispatcher dispatcher
        {
            get
            {
                return m_Dispatcher == null ? (m_Dispatcher = GetComponent<Dispatcher>()) : m_Dispatcher;
            }
        }


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
        
        private EffectsChannel m_CommonChannel;
        public Effector target { get; private set; }

        private void Awake()
        {
            target = this;
            
            /* Define common channel */
            m_CommonChannel = CreateChannel(this, "Default");
        }

        public EffectsChannel CreateChannel(Behaviour owner, string debugName)
        {
            var channel = new EffectsChannel(this, owner, debugName);
            channels.Add(channel);
#if UNITY_EDITOR
            editor_EmitChannelsChange();
#endif
            return channel;
        }
        
        /*[Obsolete("Apply effects without props is obsolete", true)]
        public EffectCancellableOperationAwaiter ApplyEffect(IEffect<TTarget> effect)
        {
            return m_CommonChannel.Add(effect);
        }*/

        public EffectCancellableOperationAwaiter ApplyEffect<TEffector, TEffectProps>(IEffectWithProps<TEffector, TEffectProps> effect, TEffectProps props = default) where TEffectProps: struct where TEffector: Effector
        {
            return m_CommonChannel.Add(effect, props);
        }
    }
}