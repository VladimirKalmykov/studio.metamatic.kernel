namespace Metamatic.Framework.Workshop
{
    /// <summary>
    /// The kind of damage that is being dealt when one object hits another.
    /// </summary>
    public readonly struct DamageEffect
    {
        public readonly ImpactType Type;
        public readonly float Quantity;
        
        public DamageEffect(ImpactType type, float quantity)
        {
            this.Type = type;
            this.Quantity = quantity;
        }
    }
}