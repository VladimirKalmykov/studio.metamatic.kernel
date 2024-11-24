using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Metamatic;
using UnityEngine;

namespace Metamatic
{
    /// <summary>
    /// Helper class to manage effects on GameObject. It is used only if someone watches for effects on the certain game object via Effect.Watch method
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class GameObjectEffectsContainer<T> where T: struct
    {
        public event System.Action<GameObject, GameObject, T> OnInstantCast;
        public event System.Action<GameObject, GameObject, EffectCast<T>> OnLongLastingCast;
        public readonly HashSet<EffectCast<T>> ActiveEffects = new HashSet<EffectCast<T>>();
        
        public void AddEffect(EffectCast<T> effect, GameObject caster, GameObject target)
        {
            ActiveEffects.Add(effect);
            OnLongLastingCast?.Invoke(caster, target, effect);
            effect.cancellationSignal.Register(() => ActiveEffects.Remove(effect));
        }
        
        public void OnCastInstant(GameObject caster, GameObject target, T data)
        {
            OnInstantCast?.Invoke(caster, target, data);
        }
    }
    
    public static class Effect<T>
    where T: struct
    {
        private static event System.Action<GameObject, GameObject, T> OnInstantCast;
        private static event System.Action<GameObject, GameObject, EffectCast<T>> OnLongLastingCast;
        private static readonly Dictionary<GameObject, GameObjectEffectsContainer<T>> GameObjectEffects = new Dictionary<GameObject, GameObjectEffectsContainer<T>>();
        
        /// <summary>
        /// Cast a spell synchronously
        /// </summary>
        /// <param name="caster"></param>
        /// <param name="target"></param>
        /// <param name="data"></param>
        public static void Cast(GameObject caster, GameObject target, T data = default)
        {
            if (GameObjectEffects.TryGetValue(caster, out var container))
            {
                container.OnCastInstant(caster, target, data);
            }
            Console.Log("Effect", "Cast",  typeof(T).Name, $"From {caster} to {target}");
            OnInstantCast?.Invoke(caster, target, data);
        }
        
        /// <summary>
        /// The same as Cast(GameObject caster, GameObject target, T data = default) but with different order of arguments.
        /// Allows to cast a spell with no target (target by default is caster)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="caster"></param>
        /// <param name="target"></param>
        public static void Cast(T data, GameObject caster, GameObject target = null)
        {
            if (target == null) target = caster;
            Cast(caster, target, data);
        }
        
        /// <summary>
        /// Cast a spell asynchronously (for long running spells)
        /// </summary>
        /// <param name="caster"></param>
        /// <param name="target"></param>
        /// <param name="data"></param>
        /// <returns>SpellCast instance</returns>
        public static EffectCast<T> Broadcast(GameObject caster, GameObject target, T data = default)
        {
            var spellCast = new EffectCast<T>();
            spellCast.data = data;
            if (GameObjectEffects.TryGetValue(caster, out var container))
            {
                container.AddEffect(spellCast, caster, target);
            }

            if (target.TryGetComponent<Effects>(out var effects))
            {
                effects.AddEffect(spellCast, caster, target);
            }
            Console.Log("Effect", "Broadcast",  typeof(T).Name, $"From {caster} to {target}");
            OnLongLastingCast?.Invoke(caster, target, spellCast);
            return spellCast;
        }
        
        /// <summary>
        /// Cast async effect with no target (target by default is caster)
        /// </summary>
        /// <param name="caster"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static EffectCast<T> Broadcast(GameObject caster, T data = default)
        {
            return Broadcast(caster, caster, data);
        }
        
        /// <summary>
        /// Subscribe to instant effect casts
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public static DisposableAction Subscribe(System.Action<GameObject, GameObject, T> handler)
        {
            OnInstantCast += handler;
            return new DisposableAction(() => OnInstantCast -= handler);
        }
        
        /// <summary>
        /// Watch async object's long lasting effect casts. If object already has active effects, they will be passed to the handler
        /// To handle effect asynchronously and wait for effect dispose use EffectCast.cancellationToken
        /// </summary>
        /// <param name="target"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        public static DisposableAction WatchAsync(GameObject target, System.Func<GameObject, GameObject, EffectCast<T>, UniTaskVoid> handler)
        {
            if (!GameObjectEffects.TryGetValue(target, out var container))
            {
                container = new GameObjectEffectsContainer<T>();
                GameObjectEffects.Add(target, container);
            }

            void Handler(GameObject caster, GameObject target, EffectCast<T> effectCast)
            {
                handler(caster, target, effectCast).Forget();
            }

            container.OnLongLastingCast += Handler;
            foreach (var activeEffect in container.ActiveEffects.ToArray())
            {
                Handler(target, target, activeEffect);
            }
            return new DisposableAction(() => container.OnLongLastingCast -= Handler);
        }
        
        /// <summary>
        /// Subscribe to long lasting spell casts for all objects
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public static DisposableAction Subscribe(System.Func<GameObject, GameObject, EffectCast<T>, UniTaskVoid> handler)
        {
            void Handler(GameObject caster, GameObject target, EffectCast<T> effectCast)
            {
                handler(caster, target, effectCast).Forget();
            }
            OnLongLastingCast += Handler;
            return new DisposableAction(() => OnLongLastingCast -= Handler);
        }
    }
}