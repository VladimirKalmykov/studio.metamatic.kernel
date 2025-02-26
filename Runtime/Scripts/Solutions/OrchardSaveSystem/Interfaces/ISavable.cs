using Cysharp.Threading.Tasks;

namespace Metamatic.SaveSystem
{
    public interface ISavable
    {
        public object Save();
        public UniTask Load(object data);
        public UniTask Bootstrap();
    }
}