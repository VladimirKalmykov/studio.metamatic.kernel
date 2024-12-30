using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public abstract class EffectVisualizer<T>: AsyncBehaviour
    where T: struct
    {
        protected override async UniTask Run(Ready ready, Signal signal)
        {
            using var listener = Effect<T>.Subscribe(OnEffect);
            await OnEffectVisualizerEnabled(ready, signal);
        }
        
        private void OnEffectInternal(GameObject caster, GameObject target, EffectCast<T> effect)
        {
            OnEffect(caster, target, effect).Forget();
        }
        
        protected abstract UniTask OnEffectVisualizerEnabled(Ready ready, Signal signal);

        protected abstract UniTaskVoid OnEffect(GameObject caster, GameObject target, EffectCast<T> effect);
    }
    
    public abstract class AsyncEffectVisualizer<T>: AsyncBehaviour
    where T: struct
    {
        protected override async UniTask Run(Ready ready, Signal signal)
        {
            using var listener = Effect<T>.Subscribe(OnEffect);
            await OnEffectVisualizerEnabled(ready, signal);
        }
        
        private void OnEffectInternal(GameObject caster, GameObject target, EffectCast<T> effect)
        {
            OnEffect(caster, target, effect).Forget();
        }
        
        protected abstract UniTask OnEffectVisualizerEnabled(Ready ready, Signal signal);

        protected abstract UniTaskVoid OnEffect(GameObject caster, GameObject target, EffectCast<T> effect);
    }
}