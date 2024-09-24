using System;
using Metamatic.Effects;
using Object = UnityEngine.Object;

namespace Metamatic.Framework.Workshop
{

    [Spell("Heal", "Heals the target for a certain amount of health")]
    public struct HealSpell: ISyncSpell
    {
        public float Hp;
        public bool OverHeal;
        
        public void OnCast(Object spellCaster, Object target)
        {
            /* Try convert target to Health, if it fails, get the Health component from the target */
            var health = Spell.ResolveComponent<Health>(target);
            if (Hp > 0)
            {
                health.TakeHeal(Hp, OverHeal);
                Effect<HealEffect>.Instant(spellCaster, health.gameObject, new HealEffect(Hp));
            }
        }

        public Exception CanCast(Object spellCaster, Object target)
        {
            return target == null || Spell.ResolveComponent<Health>(target) == null ? new Exception("Target does not have Health component") : null;
        }

        public override string ToString()
        {
            return $"Heal({Hp})";
        }
    }
}