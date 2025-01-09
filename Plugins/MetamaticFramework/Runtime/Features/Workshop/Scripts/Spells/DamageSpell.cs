namespace Metamatic.Framework.Workshop
{
    [Spell("Damage", "Deals damage to the target")]
    public struct DamageSpell
    {
        public float Damage;
        public ImpactType ImpactType;

        public override string ToString()
        {
            return $"Damage({Damage})";
        }
    }
}