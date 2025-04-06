using Cysharp.Threading.Tasks;

namespace Metamatic.SaveSystem
{
    /// <summary>
    /// Support for saving and loading data for a specific place.
    /// </summary>
    public interface ISavablePlace
    {
        public object SaveToPlace(string placeAddress);
        public UniTask LoadFromPlace(string placeAddress, object data);
    }
}