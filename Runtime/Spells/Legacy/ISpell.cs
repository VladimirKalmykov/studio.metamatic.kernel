using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public interface ISpell
    {
        public Exception CanCast(GameObject spellCaster, GameObject target);
    }
    public interface ISyncSpell: ISpell
    {
        public void OnCast(GameObject spellCaster, GameObject target);
    }

    public interface IAsyncSpell: ISpell
    {
        public UniTask OnCast(GameObject spellCaster, GameObject target, Signal signal);
    }
}