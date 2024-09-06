namespace Metamatic.Effects
{
    public interface IEffectConsumer
    {
        public void OnEffectReceived(IEffect effect);
    }
}