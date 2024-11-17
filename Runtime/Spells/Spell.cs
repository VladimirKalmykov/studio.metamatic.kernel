using System;
using System.Collections.Generic;
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
                Console.Error("Spell", "Sync", e.Message, e.StackTrace);
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
            var spellSource = ParamLessPool<SpellSourceReusable<TSpell>>.GetShared();
            return spellSource.Cast(spell, spellCaster, target);
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
    
    internal class SpellSourceReusable<TSpell>: ISpellSource<TSpell>
    where TSpell: IAsyncSpell
    {
        private TSpell m_Spell;
        private CancellationSignalSource m_CancellationSignalSource;
        public int version { get; private set; }
        public Object spellCaster { get; private set; }
        public ref TSpell GetSpellReference()
        {
            return ref m_Spell;
        }

        public CancellationSignal cancellationSignal => m_CancellationSignalSource.Signal;

        public void Modify(ISpellSource<TSpell>.SpellModifier modifier)
        {
            modifier(ref m_Spell);
        }

        public void Init()
        {
            m_CancellationSignalSource = CancellationSignalSource.Create();
        }

        public SpellCast<TSpell> Cast(TSpell spell, Object spellCaster, Object target)
        {
            Console.Info("Spell", "Async", $"Cast {spell.GetType().Name} to {target.name}");
            this.spellCaster = spellCaster;
            m_Spell = spell;
            
            return new SpellCast<TSpell>(this, target);
        }
        
        public void Release()
        {
            unchecked
            {
                version++;
            }
            spellCaster = default;
            m_Spell = default;
            m_CancellationSignalSource.TryCancel();
            ParamLessPool<SpellSourceReusable<TSpell>>.ReturnShared(this);
        }
    }

    public class SpellSource<TSpell>: ISpellSource<TSpell>, IDisposable, IExposedDisposed
        where TSpell : IAsyncSpell
    {
        private TSpell m_Spell;
        private CancellationSignalSource m_CancellationSignalSource = CancellationSignalSource.Create();

        public SpellSource(TSpell spell, Object spellCaster)
        {
            m_Spell = spell;
            this.spellCaster = spellCaster;
        }
        
        public SpellCast<TSpell> Cast(Object target)
        {
            Console.Info("Spell", "Async", $"Cast {m_Spell.GetType().Name} to {target.name}");
            
            return new SpellCast<TSpell>(this, target);
        }

        public int version { get; private set; } = 0;
        public Object spellCaster { get; }

        public ref TSpell GetSpellReference()
        {
            return ref m_Spell;
        }

        public CancellationSignal cancellationSignal => m_CancellationSignalSource.Signal;

        public void Modify(ISpellSource<TSpell>.SpellModifier modifier)
        {
            modifier(ref m_Spell);
        }

        public void Release()
        {
            /* Do nothing, because we are not pooling */
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
                isDisposed = true;
            }
            unchecked
            {
                version++;
            }
            m_CancellationSignalSource.TryCancel();
        }

        public bool isDisposed { get; private set;  }
        public event Action OnDisposed;
    }
}