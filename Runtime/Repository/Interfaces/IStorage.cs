using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public interface IStorage
    {
        public enum TypeVersionControl
        {
            None,
            /// <deprecated>Use Orchard instead</deprecated>
            /// <summary>
            /// Uses legacy metadata container that collect type version. Works only for single type and non-suitable for multi-type serialization.
            /// </summary>
            LegacyMetadataContainer,
            /// <summary>
            /// Use NewtonSoft.Json converter to control serialization version. Suitable for multi-type serialization. And contains mechanism to make final json very compact.
            /// </summary>
            Orchard
        }
        public delegate T DataFactory<out T>();
        // Свойства
        bool enabled { get; set; }
        TypeVersionControl typeVersionControl { get; }

        // Методы
        UniTask<TData> Get<TData>(string address, CancellationToken token);
        UniTask<(TData data, bool exists)> TryGet<TData>(string address, DataFactory<TData> getDefault, CancellationToken token);
        UniTask Set<TData>(string address, TData value, CancellationToken token);
        UniTask Delete<TData>(string address, CancellationToken token);
        UniTask<bool> Exists<TData>(string address, CancellationToken token);
    }
}