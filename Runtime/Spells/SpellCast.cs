using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Metamatic
{
    public readonly struct SpellCast<TSpell>: IDisposable
    where TSpell: IAsyncSpell
    {
        private readonly ISpellSource<TSpell> m_AsyncSpell;
        private readonly CancellationSignalSource m_CancellationSignalSource;
        private readonly int m_Version;
        /// <summary>
        /// The task that represents the spell cast
        /// </summary>
        public readonly UniTask CastTask;
        
        public SpellCast(ISpellSource<TSpell> asyncSpell, Object target)
        {
            m_AsyncSpell = asyncSpell;
            m_Version = m_AsyncSpell.version;
            var cancellationSignalSource = CancellationSignalSource.CreateLinked(asyncSpell.cancellationSignal);
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
            m_CancellationSignalSource.TryCancel();
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