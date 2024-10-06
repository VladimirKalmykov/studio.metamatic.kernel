using System;
using Metamatic;
using Metamatic.Effects;
using Object = UnityEngine.Object;

namespace Metamatic.Framework.Workshop
{
    [Spell("Damage", "Deals damage to the target")]
    public struct DamageSpell: ISyncSpell
    {
        public float Damage;
        public ImpactType ImpactType;
        
        public void OnCast(Object spellCaster, Object target)
        {
            var health = Spell.ResolveComponent<Health>(target);
            if (health == null)
            {
                throw new SpellCastException(this, "Target does not have Health component");
            }
            if (Damage <= 0)
            {
                return;
            }
            health.TakeDamage(spellCaster, ImpactType, Damage);
            Effect<DamageEffect>.Spawn(spellCaster, health.gameObject, new DamageEffect(ImpactType, Damage));
        }

        public Exception CanCast(Object spellCaster, Object target)
        {
            if (Damage < 0)
            {
                return new Exception("Damage cannot be negative");
            }
            return target == null || Spell.ResolveComponent<Health>(target) == null ? new Exception("Target does not have Health component") : null;
        }

        public override string ToString()
        {
            return $"Damage({Damage})";
        }
    }
}