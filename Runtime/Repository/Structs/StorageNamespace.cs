using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic.Structs
{
    public readonly struct StorageNamespace: IStorage
    {
        private readonly IStorage m_Source;
        public readonly string nameSpace;

        public bool enabled
        {
            get => m_Source.enabled;
            set => m_Source.enabled = value;
        }
        public IStorage.TypeVersionControl typeVersionControl => m_Source.typeVersionControl;

        public StorageNamespace(IStorage storage, string nameSpace)
        {
            m_Source = storage;
            this.nameSpace = nameSpace;
        }
        public UniTask<TData> Get<TData>(string address, CancellationToken token)
        {
            var newAddress = Path.Combine(nameSpace, address);
            return m_Source.Get<TData>(newAddress, token);
        }

        public UniTask<TData> TryGet<TData>(string address, IStorage.DataFactory<TData> getDefault, CancellationToken token)
        {
            var newAddress = Path.Combine(nameSpace, address);
            return m_Source.TryGet(newAddress, getDefault, token);
        }

        public UniTask Set<TData>(string address, TData value, CancellationToken token)
        {
            var newAddress = Path.Combine(nameSpace, address);
            return m_Source.Set(newAddress, value, token);
        }

        public UniTask Delete<TData>(string address, CancellationToken token)
        {
            var newAddress = Path.Combine(nameSpace, address);
            return m_Source.Delete<TData>(newAddress, token);
        }

        public UniTask<bool> Exists<TData>(string address, CancellationToken token)
        {
            var newAddress = Path.Combine(nameSpace, address);
            return m_Source.Exists<TData>(newAddress, token);
        }
    }
}