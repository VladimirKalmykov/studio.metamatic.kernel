using System;
using Cysharp.Threading.Tasks;
using Object = UnityEngine.Object;

namespace Metamatic
{
    public interface ISpell
    {
        public Exception CanCast(Object spellCaster, Object target);
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