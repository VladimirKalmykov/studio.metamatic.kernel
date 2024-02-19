using System.Threading;
using Metamatic;
using UnityEngine;

namespace Metamatic
{
    public class Repository: AsyncBehaviour
    {
        private static Repository m_Instance;
        public static Repository instance
        {
            get
            {
                if (m_Instance == null)
                {
                    
                    m_Instance = new GameObject("Repository").AddComponent<Repository>();
                    m_Instance.Initialize();
#if UNITY_EDITOR
                    m_Instance.gameObject.hideFlags = HideFlags.DontSaveInEditor & HideFlags.DontSave;
#endif
                }
                return m_Instance;
            }
        }
        
        public readonly ObservableWritableValue<IOSource> m_Source = new ObservableWritableValue<IOSource>(new FsJsonSource());
        public IOSource source
        {
            get => m_Source.value;
            private set => m_Source.value = value;
        }
        
        public AsyncReadOperation<T> Read<T>(string address, IOSource source, CancellationToken token = default)
        {
            return source.Read<T>(address, token);
        }
        
        public AsyncWriteOperation<T> Write<T>(string address, T value, IOSource source, CancellationToken token = default)
        {
            return source.Write<T>(address, value, token);
        }

        private void Initialize()
        {
            source = new FsJsonSource();
        }

        protected override void RunAsync(CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}