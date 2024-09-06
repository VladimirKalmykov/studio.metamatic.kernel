using System;
using UnityEngine;

namespace Metamatic.Effects
{
    public class EffectConsumer: MonoBehaviour, IEffectConsumer
    {
        public event Action<IEffect> OnEffectReceivedEvent;
        public void OnEffectReceived(IEffect effect)
        {
            OnEffectReceivedEvent?.Invoke(effect);
        }
        
        public DisposableAction SubscribeToEffect<T>(Action<Effect<T>> handler)
        where T: struct
        {
            void Handler(IEffect effect)
            {
                if (effect is Effect<T> effectT)
                {
                    handler(effectT);
                }
            }
            OnEffectReceivedEvent += Handler;
            return new DisposableAction(() => OnEffectReceivedEvent -= Handler);
        }
    }
}