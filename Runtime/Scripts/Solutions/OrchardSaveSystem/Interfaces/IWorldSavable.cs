using Cysharp.Threading.Tasks;

namespace Metamatic.SaveSystem
{
    /// <summary>
    /// Services implementing this interface will be loaded after game world is initialized. It allows you to have different save data for each world.
    /// </summary>
    public interface IWorldSavable
    {
        public object SaveToWorld();
        public UniTask LoadFromWorld(object data);
    }
}