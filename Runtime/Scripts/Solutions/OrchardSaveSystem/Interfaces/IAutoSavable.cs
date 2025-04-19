using Cysharp.Threading.Tasks;

namespace Metamatic.SaveSystem
{
    public interface IAutoSavable
    {
        public object Save();
        public UniTask Load(object data);
    }
}