using System;
using UnityEngine;

namespace Metamatic
{
    public class DatabaseVirtualStore<TState>: IDisposable
    {
        public readonly DatabaseSelector databaseSelector;
        public delegate void StateChangeHandler(ref TState state, ref TState oldState);
        public delegate void StateModifier(ref TState state);
        /// <summary>
        /// Used for unsubscribe branch
        /// </summary>
        private Action m_UnsubscribeBranch;
        private ObservableValue<DatabaseStore<TState>> m_ObservableStore = new ObservableValue<DatabaseStore<TState>>();
        
        #if DEBUG
        public ObservableValue<DatabaseStore<TState>> debugObservableStore
        {
            get
            {
                return m_ObservableStore;
            }
        }
        #endif

        /// <summary>
        /// Contains reference to the available store (it can be nell if store does not exist yet)
        /// </summary>
        private DatabaseStore<TState> m_CurrentStoreRef = null;

        private TState initialState;
       
        /// <summary>
        /// Determines that current store object is belong to required database entity (without bubbling proxy)
        /// </summary>
        public bool isOriginal
        {
            get;
            private set;
        } = false;

        public readonly int id;
        private bool m_Disposed = false;
        private event Action OnDispose;
        

        public DatabaseVirtualStore(DatabaseSelector selector, TState initialState = default)
        {
            this.id = UnityEngine.Random.Range(0, 99999999);
            this.databaseSelector = selector;
            this.initialState = initialState;
            OnDispose += selector.Subscribe(ResolveStore);
        }

        ~DatabaseVirtualStore()
        {
            Debug.Log("Destruct Virtual Store");
            Dispose();
        }

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            m_UnsubscribeBranch?.Invoke();
        }

        private void ResolveStore(string branchName, string entityName)
        {
            m_UnsubscribeBranch?.Invoke();
            m_UnsubscribeBranch = Database.SubscribeStore<TState>(branchName, entityName, (DatabaseStore<TState> store) =>
            {
                isOriginal = store != null && (store.entity.address == databaseSelector.address);

                if (store != m_ObservableStore.value)
                {
                    m_ObservableStore.SetValue(store);
                }
                
                if (!isOriginal)
                {
                    Database.TryLoadStore<TState>(databaseSelector.address);
                }
            });
        }
        
        /// <summary>
        /// Subscribe state change
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public Action Subscribe(DatabaseStore<TState>.StateChangeHandler handler)
        {
            DatabaseStore<TState> cachedStore = null;
            Action unsubscribeState = null;
            Action unsubscribeStore = m_ObservableStore.Subscribe(store =>
            {
                unsubscribeState?.Invoke();
                if (cachedStore != null)
                {
                    if (store != null)
                    {
                        handler(ref store.state, ref cachedStore.state);
                    }
                    else
                    {
                        handler(ref initialState, ref cachedStore.state);
                    }
                }
                else
                {
                    if (store != null)
                    {
                        handler(ref store.state, ref initialState);
                    }
                    else
                    {
                        handler(ref initialState, ref initialState);
                    }
                }

                if (store != null)
                {
                    unsubscribeState = store.SubscribeState(handler, true);
                }
            });

            return () =>
            {
                unsubscribeState?.Invoke();
                unsubscribeStore();
            };
        }
        
        /// <summary>
        /// Set state of the store. If store, or entity, or store doesn't exists it will be created
        /// </summary>
        /// <param name="modifier"></param>
        public void SetState(DatabaseStore<TState>.StateModifier modifier)
        {
            if (isOriginal)
            {
                m_ObservableStore.value.SetState(modifier);
            }
            else
            {
                var tocuhedBranch = Database.TouchBranch(databaseSelector.branch.value);
                tocuhedBranch.TouchEntity(databaseSelector.entity.value).TouchStore<TState>(ref initialState, modifier);
            }
        }

        public void SetState(TState state)
        {
            if (isOriginal)
            {
                m_ObservableStore.value.SetState(state);
            }
            else
            {
                var tocuhedBranch = Database.TouchBranch(databaseSelector.branch.value);
                tocuhedBranch.TouchEntity(databaseSelector.entity.value).TouchStore<TState>(ref initialState,
                    (ref TState currentState) =>
                    {
                        currentState = state;
                    });
            }
        }

        public ref TState GetState()
        {
            if (m_ObservableStore.value == null)
            {
                return ref initialState;
            }
            return ref m_ObservableStore.value.state;
        }

        public void Load()
        {
            Database.TryLoadStore<TState>(this.databaseSelector.address, true, initialState);
        }
    }
}