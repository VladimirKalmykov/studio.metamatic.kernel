namespace Metamatic.Framework.Workshop
{

    [Spell("Heal", "Heals the target for a certain amount of health")]
    public struct HealSpell
    {
        public float Hp;
        public bool OverHeal;

        public override string ToString()
        {
            return $"Heal({Hp})";
        }
    }
}