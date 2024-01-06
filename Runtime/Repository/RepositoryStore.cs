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
        private RepositoryObservableLayer m_TargetObservableLayer;
        private ObservableWritableValue<RepositoryInternalStore<T>> m_Store =new ();
        private ObservableWritableValue<bool> m_Busy = new();
        public RepositoryStore(RepositoryBehaviour repositoryBehaviour, RepositoryObservableLayer targetObservableLayer, string name)
        {
            m_Name = name;
            m_CancellationTokenSource = new CancellationTokenSource();
            m_RepositoryBehaviour = repositoryBehaviour;
            m_TargetObservableLayer = targetObservableLayer;
            m_RepositoryBehaviour.OnBeforeDestroy += OnBeforeRepositoryDestroy;
            
            /* Load store from storage once */
            // TODO: Combine all pull requests into one (for example if pull request already in progress, just subscribe to it)
            m_Busy.SetValue(true);
            var pullOperation = repositoryBehaviour.PullStore<T>(name, m_CancellationTokenSource.Token);
            
            /* Subscribe to current layer changes and try to resolve store every time data layer changed */
            var layerChangeHandler =
                new SequentialHandler<RepositoryLayer, object>(OnCurrentLayerChange,
                    m_CancellationTokenSource.Token);
            var unsubscribeLayer = targetObservableLayer.Subscribe(layerChangeHandler.Handle);
            
            /* Acquire observable layer */
            var disposeLayer = targetObservableLayer.Acquire(this);
            OnDispose += () =>
            {
                disposeLayer();
                layerChangeHandler.Dispose();
                unsubscribeLayer();
                
            };
            pullOperation.OnCompleted(() =>
            {
                m_Busy.SetValue(false);
            });
        }
        
        /// <summary>
        /// Initialize store data in memory with default value
        /// </summary>
        public void Initialize()
        {
            
        }

        private async void OnCurrentLayerChange(RepositoryLayer layer, CancellationToken cancellationToken, object context)
        {
            using (var layerStoreChangeHandler = new SequentialHandler<RepositoryInternalStore<T>, object>(OnLayerStoreChange, cancellationToken))
            using (new DisposableCallback(layer.InternalSubscribeStore<T>(m_Name, layerStoreChangeHandler.Handle)))
            {
                await cancellationToken.AsUniTask();
            }
        }
        
        private async void OnLayerStoreChange(RepositoryInternalStore<T> store, CancellationToken cancellationToken, object context)
        {
            m_Store.SetValue(store);
        }
        
        #region Public methods
        
        /// <summary>
        /// Get value from store synchronously. If store not found, return default value. If store not loaded from storage, return default value.
        /// </summary>
        /// <returns></returns>
        public void GetSync(out T? value)
        {
            if (m_Store.value == null)
            {
                value = null;
            }
            else
            {
                m_Store.value.Get(out value);
            }
        }
        
        /// <summary>
        /// Get value from store synchronously. If store not found, return default value. If store not loaded from storage, return default value.
        /// </summary>
        /// <returns></returns>
        public T? GetSync()
        {
            if (m_Store.value == null)
            {
               return null;
            }
            else
            {
                return m_Store.value.Get();
            }
        }
        
        /// <summary>
        /// Get value from store asynchronously. If store not loaded from storage, try to load it from storage.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async UniTask<T?> GetAsync(CancellationToken cancellationToken)
        {
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
                return null;
            }
            else
            {
                return GetSync();
            }
        }
        
        public void Set(T value)
        {
            if (!m_TargetObservableLayer.isOriginal)
            {
                /* Write operation on not original layer will throw exception. User must create layer before first write operation */
                throw new Exception($"Cannot write to store in non-original layer with name \"{m_TargetObservableLayer.name}\". Checkout layer first.");
            }
            if (m_Store.value != null)
            {
                m_Store.value.Set(value);
                return;
            }
            /* Write operation on not loaded store will force create store in current layer */
            if (!m_TargetObservableLayer.value.InternalTryGetStore<T>(m_Name, out var store, true, value))
            {
                store.Set(value);
            }
        }
        
        /// <summary>
        /// Subscribe to value changes. If store not loaded from storage, try to load it from storage.
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public Action Subscribe(Action<T?> handler)
        {
            var localCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(m_CancellationTokenSource.Token);
            var storeHandler = new SequentialHandler< RepositoryInternalStore<T>, Action<T?>>(SubscribeStoreHandler, localCancellationTokenSource.Token, handler);
            Action currentUnsubscriber = null;
            
            /* Wait for busy task until subscription */
            if (m_Busy.value)
            {
                Action continuation = () =>
                {
                    
                    currentUnsubscriber = m_Store.Subscribe(storeHandler.Handle);
                };
                
                currentUnsubscriber = m_Busy.Subscribe((bool isBusy) =>
                {
                    if (!isBusy)
                    {
                        currentUnsubscriber?.Invoke();
                        currentUnsubscriber = null;
                        continuation();
                        continuation = null;
                    }
                });
            }
            else
            {
                currentUnsubscriber = m_Store.Subscribe(storeHandler.Handle);
            }
            
            /* Immediate get value */
            GetSync(out var value);
            
            handler(value);

            return () =>
            {
                currentUnsubscriber?.Invoke();
                localCancellationTokenSource.Cancel();
                storeHandler.Dispose();
            };
        }
        
        private void SubscribeStoreHandler(RepositoryInternalStore<T> store, CancellationToken cancellationToken, Action<T?> dataChangeHandler)
        {
            if (store == null)
            {
                dataChangeHandler(null);
                return;
            }
            else
            {
                dataChangeHandler(store.Get());
            }
            var unsubscribe = store.Subscribe(dataChangeHandler);
            OnDispose += unsubscribe;
            cancellationToken.Register(() =>
            {
                OnDispose -= unsubscribe;
                unsubscribe();
            });
        }
        
        #endregion
    }
}