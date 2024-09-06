using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Metamatic.Effects
{
    public sealed class Effector
    {
        private readonly HashSet<IEffect> m_Effects = new HashSet<IEffect>();
        public event Action<IEffect> OnEffectAdded;

        public IEffect Apply(IEffect effect)
        {
            InternalApply(effect).Forget();
            return effect;
        }
        
        public DisposableAction Subscribe(Action<IEffect> action)
        {
            OnEffectAdded += action;
            foreach (var effect in m_Effects)
            {
                action(effect);
            }
            return new DisposableAction(() => OnEffectAdded -= action);
        }
        
        private async UniTaskVoid InternalApply(IEffect effect)
        {
            m_Effects.Add(effect);
            OnEffectAdded?.Invoke(effect);
            if (!effect.instant)
            {
                await effect.cancellationSignal;
            }
            m_Effects.Remove(effect);
        }
    }
}