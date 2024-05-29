using System;
using System.Collections.Generic;
using System.Threading;

namespace Metamatic
{
    public class SequentialMapHandler<TKey>: IDisposable
    {
        private bool m_Disposed;
        protected Dictionary<TKey, CancellationTokenSource> _cancellationTokenSources = new Dictionary<TKey, CancellationTokenSource>();
        ~SequentialMapHandler()
        {
            Dispose();
        }
        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            foreach (var item in _cancellationTokenSources)
            {
                if (m_Disposed) return;
                m_Disposed = true;
                item.Value.Cancel();
                item.Value.Dispose();
            }
            _cancellationTokenSources.Clear();
            GC.SuppressFinalize(this);
        }
    }
    
    public class SequentialMapHandler<TKey, TValue>: SequentialMapHandler<TKey>
    {
        public delegate void Handler(TKey key, TValue value, CancellationToken cancellationToken);
        private Handler m_Handler;
        
        
        public SequentialMapHandler(Handler handler)
        {
            m_Handler = handler;
        }
        
        public void Handle(TKey key, TValue value)
        {
            if (_cancellationTokenSources.ContainsKey(key))
            {
                _cancellationTokenSources[key].Cancel();
                _cancellationTokenSources[key].Dispose();
            }
            var cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokenSources[key] = cancellationTokenSource;
            m_Handler(key, value, cancellationTokenSource.Token);
        }
    }
}