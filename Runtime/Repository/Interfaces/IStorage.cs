using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public interface IStorage
    {
        public enum TypeVersionControl
        {
            Disabled,
            Enabled,
            Auto
        }
        public delegate T DataFactory<out T>();
        // Свойства
        bool enabled { get; set; }
        TypeVersionControl typeVersionControl { get; }

        // Методы
        UniTask<TData> Get<TData>(string address, CancellationToken token);
        UniTask<TData> TryGet<TData>(string address, DataFactory<TData> getDefault, CancellationToken token);
        UniTask Set<TData>(string address, TData value, CancellationToken token);
        UniTask Delete<TData>(string address, CancellationToken token);
        UniTask<bool> Exists<TData>(string address, CancellationToken token);
    }
}