using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public readonly struct SpellCastLehacy<TSpell>: IDisposable
    where TSpell: IAsyncSpell
    {
        private readonly ISpellSource<TSpell> m_AsyncSpell;
        private readonly SignalSource m_CancellationSignalSource;
        private readonly int m_Version;
        /// <summary>
        /// The task that represents the spell cast
        /// </summary>
        public readonly UniTask CastTask;
        
        public SpellCastLehacy(ISpellSource<TSpell> asyncSpell, GameObject target)
        {
            m_AsyncSpell = asyncSpell;
            m_Version = m_AsyncSpell.version;
            var cancellationSignalSource = SignalSource.CreateLinked(asyncSpell.cancellationSignal);
            m_CancellationSignalSource = cancellationSignalSource;
            var spellReference = asyncSpell.GetSpellReference();
            CastTask = spellReference.OnCast(asyncSpell.spellCaster, target, cancellationSignalSource.Signal);
        }
        
        public void Modify(ISpellSource<TSpell>.SpellModifier modifier)
        {
            GetSpell()?.Modify(modifier);
        }

        public void Dispose()
        {
            m_CancellationSignalSource.TryRaise();
            GetSpell()?.Release();
        }
        
        private ISpellSource<TSpell> GetSpell()
        {
            if (m_AsyncSpell == null || m_AsyncSpell.version != m_Version)
            {
                return null;
            }
            return m_AsyncSpell;
        }
    }
}