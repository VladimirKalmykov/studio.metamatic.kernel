using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Metamatic.DevTools;
using Action = System.Action;

namespace Metamatic.Effects
{   
    public class Effector : MonoBehaviour
    {
        public event Action OnBeforeDestroy;
        public readonly List<IEffect.EffectExecutionContext> effectExecutions = new List<IEffect.EffectExecutionContext>();
        public readonly Dictionary<string, EffectsChannel> channels = new Dictionary<string, EffectsChannel>();

        // public Dispatcher dispatcher
        // {
        //     get
        //     {
        //         return m_Dispatcher == null ? (m_Dispatcher = GetComponent<Dispatcher>()) : m_Dispatcher;
        //     }
        // }
        private static Effector _globalEffector;
        /// <summary>
        /// Reference to the global effector
        /// </summary>
        public static Effector global => _globalEffector ??= new GameObject("__EFFECTOR__").AddComponent<Effector>();


#if UNITY_EDITOR
        public event System.Action<EffectsChannel[]> editor_OnChannelsChange;
        public void editor_SubscribeChannels(System.Action<EffectsChannel[]> handler)
        {
            handler(channels.Values.ToArray());
            editor_OnChannelsChange += handler;
        }
        
        public void editor_UnsubscribeChannels(System.Action<EffectsChannel[]> handler)
        {
            editor_OnChannelsChange -= handler;
        }

        protected void editor_EmitChannelsChange()
        {
            editor_OnChannelsChange?.Invoke(channels.Values.ToArray());
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

        public EffectsChannel commonChannel
        {
            get
            {
                if (m_CommonChannel == null)
                {
                    m_CommonChannel = CreateChannel(this, "Default");
                }

                return m_CommonChannel;
            }
        }

        public Effector target => this;

        private EffectsChannel CreateChannel(Behaviour owner, string debugName)
        {
            var channel = new EffectsChannel(this, owner, debugName);
            channels.Add(debugName, channel);
#if UNITY_EDITOR
            editor_EmitChannelsChange();
#endif
            return channel;
        }

        public EffectsChannel TouchChannel(string channelName)
        {
            EffectsChannel channel = null;
            if (!channels.TryGetValue(channelName, out channel))
            {
                return CreateChannel(this, channelName);
            }

            return channel;
        }
        
        /*[Obsolete("Apply effects without props is obsolete", true)]
        public EffectCancellableOperationAwaiter ApplyEffect(IEffect<TTarget> effect)
        {
            return m_CommonChannel.Add(effect);
        }*/

        public EffectCancellableOperationAwaiter ApplyEffect<TEffector, TEffectProps, TResult>(IEffect<TEffector, TEffectProps, TResult> effect, TEffectProps props = default) where TEffectProps: struct where TEffector: Effector
        {
            return commonChannel.Add(effect, props);
        }
        
        /// <summary>
        /// Cancel all effects
        /// </summary>
        public void CancelAllEffects()
        {
            var executions = channels.Values.SelectMany(channel => channel.executions).ToArray();
            foreach (var execution in executions)
            {
                execution.Dispose();
            }
        }
    }
}