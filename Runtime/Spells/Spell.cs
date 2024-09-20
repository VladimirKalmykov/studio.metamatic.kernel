using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Metamatic
{
    public class Spell
    {
        /// <summary>
        /// Cast a spell synchronously
        /// </summary>
        /// <param name="syncSpell"></param>
        /// <param name="spellCaster"></param>
        /// <param name="target"></param>
        public static void Cast(ISyncSpell syncSpell, Object spellCaster, Object target)
        {
            Console.Info("Spell", "Sync", $"Cast {syncSpell} to {target.name}");
            try
            {
                syncSpell.OnCast(spellCaster, target);
            }
            catch (SpellCastException e)
            {
                Console.Error("Spell", "Sync", e.SpellType.Name, e.Message);
            }
            catch (Exception e)
            {
                Console.Error("Spell", "Sync", e.Message);
            }
        }
        /// <summary>
        /// Cast a spell asynchronously
        /// </summary>
        /// <param name="spell"></param>
        /// <param name="spellCaster"></param>
        /// <param name="target"></param>
        /// <param name="signal"></param>
        /// <typeparam name="TSpell"></typeparam>
        /// <returns></returns>
        public static SpellCast<TSpell> Cast<TSpell>(TSpell spell, Object spellCaster, Object target, CancellationSignal signal) where TSpell: IAsyncSpell
        {
            Console.Info("Spell", "Async", $"Cast {spell.GetType().Name} to {target.name}");
            var spellSource = ParamLessPool<AsyncSpell<TSpell>>.GetShared();
            spellSource.Cast(spell, spellCaster, target);
            return new SpellCast<TSpell>(spellSource);
        }
        
        public static T ResolveComponent<T>(Object obj) where T: Component
        {
            if (obj is T component)
            {
                return component;
            }
            if (obj is GameObject gameObject)
            {
                return gameObject.GetComponent<T>();
            }
            if (obj is Component componentObj)
            {
                return componentObj.GetComponent<T>();
            }

            return null;
        }
    }
    
    public class AsyncSpell<TSpell>
    where TSpell: IAsyncSpell
    {
        public delegate void SpellModifier(ref TSpell spell);
        private TSpell m_Spell;
        public int version { get; private set; }
        private CancellationSignalSource m_CancellationSignalSource;
        public void Cast(TSpell spell, Object spellCaster, Object target)
        {
            m_Spell = spell;
            m_Spell.OnCast(spellCaster, target, m_CancellationSignalSource.Signal);
        }
        
        public void Release()
        {
            m_CancellationSignalSource.TryCancel();
            version++;
            m_Spell = default;
            ParamLessPool<AsyncSpell<TSpell>>.ReturnShared(this);
        }
        
        public void Modify(SpellModifier modifier)
        {
            modifier(ref m_Spell);
        }
    }
}