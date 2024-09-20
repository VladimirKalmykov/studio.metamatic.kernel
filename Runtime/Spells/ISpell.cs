using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public interface ISpell
    {
    }
    public interface ISyncSpell: ISpell
    {
        public void OnCast(Object spellCaster, Object target);
    }

    public interface IAsyncSpell: ISpell
    {
        public UniTask OnCast(Object spellCaster, Object target, CancellationSignal signal);
    }
}