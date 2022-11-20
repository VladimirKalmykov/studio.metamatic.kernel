using System;
using System.Collections.Generic;

namespace Metamatic
{
    public class DatabaseVirtualStore<TState>
    {
        public readonly DatabaseSelector databaseSelector;
        public delegate void StateChangeHandler(ref TState state, ref TState oldState);
        public delegate void StateModifier(ref TState state);
        /// <summary>
        /// Used for unsubscribe branch
        /// </summary>
        private Action m_UnsubscribeBranch;
        private ObservableValue<DatabaseStore<TState>> m_ObservableStore = new ObservableValue<DatabaseStore<TState>>();

        /// <summary>
        /// Contains reference to the available store (it can be nell if store does not exist yet)
        /// </summary>
        private DatabaseStore<TState> m_CurrentStoreRef = null;

        private TState initialState;
        

        public DatabaseVirtualStore(DatabaseSelector selector, TState initialState = default)
        {
            this.databaseSelector = selector;
            this.initialState = initialState;
            selector.Subscribe(ResolveStore);
        }

        private void ResolveStore(string branchName, string entityName)
        {
            m_UnsubscribeBranch?.Invoke();
            m_UnsubscribeBranch = Database.SubscribeStore<TState>(branchName, entityName, (DatabaseStore<TState> store) =>
            {
                m_ObservableStore.SetValue(store);
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
                    unsubscribeState = store.SubscribeState(handler);
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
            if (m_ObservableStore.value != null)
            {
                m_ObservableStore.value.SetState(modifier);
            }
            else
            {
                Database.TouchBranch(databaseSelector.branch.value).TouchEntity(databaseSelector.entity.value).TouchStore<TState>().SetState(modifier);
            }
        }
    }
}