using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public class RepositoryStore
    {
        public event Action OnDispose;
        /// <summary>
        /// Personal cancellation token source for this store, used to cancel async operations when store destroyed
        /// </summary>
        protected CancellationTokenSource m_CancellationTokenSource;
        protected RepositoryBehaviour m_RepositoryBehaviour;
        private int m_ReserveCount;
        
        ~RepositoryStore()
        {
            Dispose();
        }
        protected void OnBeforeRepositoryDestroy()
        {
            Dispose();
            GC.SuppressFinalize(this);
        }

        public Action Acquire()
        {
            m_ReserveCount++;
            return Dispose;
        }
        
        public void Dispose()
        {
            m_ReserveCount--;
            if (m_ReserveCount == 0)
            {
                Release();
            }
        }

        private void Release()
        {
            OnDispose?.Invoke();
            m_CancellationTokenSource?.Cancel();
            m_RepositoryBehaviour.OnBeforeDestroy -= OnBeforeRepositoryDestroy;
        }
    }
    public class RepositoryStore<T>: RepositoryStore, IDisposable
    where T: struct
    {
        
        private string m_Name;
        private ObservableValue<RepositoryDataLayer> m_TargetLayer;
        private ObservableWritableValue<RepositoryInternalStore<T>> m_Store =new ();
        private ObservableWritableValue<bool> m_Busy = new();
        private T m_DefaultValue;
        public RepositoryStore(RepositoryBehaviour repositoryBehaviour, ObservableValue<RepositoryDataLayer> targetLayer, string name, T defaultValue = default)
        {
            m_RepositoryBehaviour = repositoryBehaviour;
            m_TargetLayer = targetLayer;
            m_DefaultValue = defaultValue;
            m_RepositoryBehaviour.OnBeforeDestroy += OnBeforeRepositoryDestroy;
            
            /* Load store from storage */
            m_Busy.SetValue(true);
            repositoryBehaviour.PullStore<T>(name, CancellationToken.None).OnCompleted(() =>
            {
                m_Busy.SetValue(false);
            });
            
            /* Subscribe to current layer changes and try to resolve store every time data layer changed */
            var layerChangeHandler =
                new SequentialHandler<RepositoryDataLayer, object>(OnCurrentLayerChange,
                    m_CancellationTokenSource.Token);
            var unsubscribeLayer = targetLayer.Subscribe(layerChangeHandler.Handle);

            OnDispose += () =>
            {
                layerChangeHandler.Dispose();
                unsubscribeLayer();
                
            };
        }

        private async void OnCurrentLayerChange(RepositoryDataLayer layer, CancellationToken cancellationToken, object context)
        {
            using (var layerStoreChangeHandler = new SequentialHandler<RepositoryInternalStore<T>, object>(OnLayerStoreChange, cancellationToken))
            using (new DisposableCallback(layer.SubscribeStore<T>(m_Name, layerStoreChangeHandler.Handle)))
            {
                await cancellationToken.AsUniTask();
            }
        }
        
        private async void OnLayerStoreChange(RepositoryInternalStore<T> store, CancellationToken cancellationToken, object context)
        {
            m_Store.SetValue(store);
        }
        
        
        
        
        #region Public methods
        
        /*
         * Get value from store synchronously. If store not found, return default value. If store not loaded from storage, return default value.
         */
        public void GetSync(out T value)
        {
            if (m_Store.value == null)
            {
                value = m_DefaultValue;
            }
            else
            {
                m_Store.value.Get(out value);
            }
        }
        
        /// <summary>
        /// Get value from store asynchronously. If store not loaded from storage, try to load it from storage.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async UniTask<T> GetAsync(CancellationToken cancellationToken)
        {
            if (m_Store.value != null)
            {
                m_Store.value.Get(out var result);
                return result;
            }
            if (m_Busy.value)
            {
                try
                {
                    await m_Busy.WaitUntilValueIs(false, cancellationToken);
                    var value = await GetAsync(cancellationToken);
                    return value;
                }
                catch (OperationCanceledException)
                {
                    // ignored
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
                return m_DefaultValue;
            }
            return m_DefaultValue;
        }
        
        public void Set(T value)
        {
            if (m_Store.value == null)
            {
                /* Write operation on not loaded store will force create store in current layer */
                m_TargetLayer.value.TryGetStore<T>(m_Name, out var store, true);
                store.Set(value);
            }
        }
        
        /// <summary>
        /// Subscribe to value changes. If store not loaded from storage, try to load it from storage.
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public Action Subscribe(Action<T> handler)
        {
            var storeHandler = new SequentialHandler< RepositoryInternalStore<T>, Action<T>>(SubscribeStoreHandler, m_CancellationTokenSource.Token, handler);
            var unSubscribe = m_Store.Subscribe(storeHandler.Handle);

            return () =>
            {
                unSubscribe();
                storeHandler.Dispose();
            };
        }
        
        private void SubscribeStoreHandler(RepositoryInternalStore<T> store, CancellationToken cancellationToken, Action<T> handler)
        {
            if (store == null)
            {
                handler(m_DefaultValue);
                return;
            }
            var unsubscribe = store.Subscribe(handler);
            OnDispose += unsubscribe;
            cancellationToken.Register(() =>
            {
                unsubscribe();
            });
        }
        
        #endregion
    }
}