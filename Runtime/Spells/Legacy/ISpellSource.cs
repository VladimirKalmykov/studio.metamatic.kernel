using UnityEngine;

namespace Metamatic
{
    public interface ISpellSource<TSpell> where TSpell: IAsyncSpell
    {
        public delegate void SpellModifier(ref TSpell spell);
        public int version { get; }
        public GameObject spellCaster { get; }
        public ref TSpell GetSpellReference();
        public CancellationSignal cancellationSignal { get; }
        public void Modify(SpellModifier modifier);
        public void Release();
    }
}