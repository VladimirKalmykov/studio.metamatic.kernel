using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Metamatic
{
    public class SpellLegacy
    {
        /// <summary>
        /// Cast a spell synchronously
        /// </summary>
        /// <param name="syncSpell"></param>
        /// <param name="spellCaster"></param>
        /// <param name="target"></param>
        public static void Cast(ISyncSpell syncSpell, GameObject spellCaster, GameObject target)
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
        /// Cast a spell asynchronously (for long running spells)
        /// </summary>
        /// <param name="spell"></param>
        /// <param name="spellCaster"></param>
        /// <param name="target"></param>
        /// <param name="signal"></param>
        /// <typeparam name="TSpell"></typeparam>
        /// <returns></returns>
        public static SpellCastLehacy<TSpell> Cast<TSpell>(TSpell spell, GameObject spellCaster, GameObject target, CancellationSignal signal) where TSpell: IAsyncSpell
        {
            var spellSource = ParamLessPool<SpellSourceReusable<TSpell>>.GetShared();
            return spellSource.Cast(spell, spellCaster, target);
        }
        
        /// <summary>
        /// Cast a spell asynchronously (for long running spells)
        /// </summary>
        /// <param name="spell"></param>
        /// <param name="spellCasterAndTarget"></param>
        /// <param name="signal"></param>
        /// <typeparam name="TSpell"></typeparam>
        /// <returns></returns>
        public static SpellCastLehacy<TSpell> Cast<TSpell>(TSpell spell, GameObject spellCasterAndTarget, CancellationSignal signal) where TSpell: IAsyncSpell
        {
            var spellSource = ParamLessPool<SpellSourceReusable<TSpell>>.GetShared();
            return spellSource.Cast(spell, spellCasterAndTarget, spellCasterAndTarget);
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
        private bool m_Casted = false;
        public int version { get; private set; }
        public GameObject spellCaster { get; private set; }
        public ref TSpell GetSpellReference()
        {
            return ref m_Spell;
        }

        public CancellationSignal cancellationSignal => m_CancellationSignalSource.Signal;

        public void Modify(ISpellSource<TSpell>.SpellModifier modifier)
        {
            modifier(ref m_Spell);
        }
        
        /// <summary>
        /// Set new spell data
        /// </summary>
        /// <param name="spell"></param>
        public void UpdateData(TSpell spell)
        {
            m_Spell = spell;
        }

        public void Init()
        {
            m_CancellationSignalSource = CancellationSignalSource.Create();
        }

        public SpellCastLehacy<TSpell> Cast(TSpell spell, GameObject spellCaster, GameObject target)
        {
            if (m_Casted)
            {
                throw new InvalidOperationException("Spell is already casted");
            }

            Init();
            m_Casted = true;
            Console.Info("Spell", "Async", $"Cast {spell.GetType().Name} to {target.name}");
            this.spellCaster = spellCaster;
            m_Spell = spell;
            
            return new SpellCastLehacy<TSpell>(this, target);
        }
        
        public void Release()
        {
            unchecked
            {
                version++;
            }
            m_Casted = false;
            spellCaster = default;
            m_Spell = default;
            m_CancellationSignalSource.TryCancel();
            ParamLessPool<SpellSourceReusable<TSpell>>.ReturnShared(this);
        }
    }

    public class SpellSource<TSpell>: ISpellSource<TSpell>, IDisposable, IExposedDisposedSync
        where TSpell : IAsyncSpell
    {
        private TSpell m_Spell;
        private CancellationSignalSource m_CancellationSignalSource = CancellationSignalSource.Create();

        public SpellSource(TSpell spell, GameObject spellCaster)
        {
            m_Spell = spell;
            this.spellCaster = spellCaster;
        }
        
        public SpellCastLehacy<TSpell> Cast(GameObject target)
        {
            Console.Info("Spell", "Async", $"Cast {m_Spell.GetType().Name} to {target.name}");
            
            return new SpellCastLehacy<TSpell>(this, target);
        }

        public int version { get; private set; } = 0;
        public GameObject spellCaster { get; }

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
            if (isDisposedSync)
            {
                return;
                isDisposedSync = true;
            }
            unchecked
            {
                version++;
            }
            m_CancellationSignalSource.TryCancel();
        }

        public bool isDisposedSync { get; private set;  }
        public event Action OnDisposedSync;
    }
}