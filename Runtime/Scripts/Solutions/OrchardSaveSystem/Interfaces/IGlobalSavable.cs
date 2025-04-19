using Cysharp.Threading.Tasks;

namespace Metamatic.SaveSystem
{
    /// <summary>
    /// Services implementing this interface will loaded before game world is initialized. It allows you to have global save data for all worlds and places.
    /// </summary>
    public interface IGlobalSavable
    {
        public object SaveToGlobal();
        public UniTask LoadFromGlobal(object data);

        public void ResetGlobalState();
    }
}