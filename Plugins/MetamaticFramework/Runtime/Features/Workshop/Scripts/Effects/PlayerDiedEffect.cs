namespace Metamatic.Framework.Workshop
{
    public readonly struct PlayerDiedEffect
    {
        public readonly Entity character;
        
        public PlayerDiedEffect(Entity character)
        {
            this.character = character;
        }
    }
}