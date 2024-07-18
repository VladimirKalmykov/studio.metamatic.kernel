using System;
using System.Collections.Generic;
using System.Threading;

namespace Metamatic
{
    /// <summary>
    /// Creates a handler that will handle the same key sequentially. Instead of SequentialHandler, this class lets you control lifecycle for each key.
    /// </summary>
    /// <example>
    /// var handler = new SequentialMapHandler<string, GameObject>(NamedObjectsHandler);
    /// handler.handle('player1', player1GameObject);
    /// handler.handle('player2', player2GameObject);
    /// handler.handle('player1', newPlayer1GameObject); // For player1, the previous handler will be canceled and the new one will be started.
    /// handler.handle('player2', newPlayer2GameObject); // For player2, the previous handler will be canceled and the new one will be started.
    /// </example>
    /// <typeparam name="TKey"></typeparam>
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