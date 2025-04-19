using Cysharp.Threading.Tasks;

namespace Metamatic.SaveSystem
{
    /// <summary>
    /// Services implementing this interface will be loaded within the place. It allows you to have different save data for each place.
    /// </summary>
    public interface IPlaceSavable
    {
        public object SaveToPlace(string placeAddress);
        public UniTask LoadFromPlace(string placeAddress, object data);
    }
}