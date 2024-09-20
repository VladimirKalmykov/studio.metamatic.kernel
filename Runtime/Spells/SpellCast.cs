using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Metamatic
{
    public readonly struct SpellCast<TSpell>: IDisposable
    where TSpell: IAsyncSpell
    {
        private readonly AsyncSpell<TSpell> m_AsyncSpell;
        public readonly int Version;
        
        public SpellCast(AsyncSpell<TSpell> asyncSpell)
        {
            m_AsyncSpell = asyncSpell;
            Version = m_AsyncSpell.version;
        }
        
        public void Modify(AsyncSpell<TSpell>.SpellModifier modifier)
        {
            GetSpell()?.Modify(modifier);
        }

        public void Dispose()
        {
            GetSpell()?.Release();
        }
        
        private AsyncSpell<TSpell> GetSpell()
        {
            if (m_AsyncSpell == null || m_AsyncSpell.version != Version)
            {
                return null;
            }
            return m_AsyncSpell;
        }
    }
}