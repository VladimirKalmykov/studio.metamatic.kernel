using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public class Store
    {
        
    }
    public class Store<T>: Store, IDisposable
    {
        private InternalStore<T> m_InternalStore;
        private List<Action> m_Subscriptions = new List<Action>();
        public T value
        {
            get => m_InternalStore.value;
            set => m_InternalStore.value = value;
        }
        public Store(InternalStore<T> store)
        {
            this.m_InternalStore = store;
            store.Asquire();
        }

        ~Store()
        {
            Dispose();
        }
        public void Dispose()
        {
            m_InternalStore.Release();
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Subscribe to store changes.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public Action Subscribe(Action<T> action)
        {
            var unsubscribe = m_InternalStore.Subscribe(action);
            m_Subscriptions.Add(unsubscribe);

            return () =>
            {
                m_Subscriptions.Remove(unsubscribe);
                unsubscribe();
            };
        }
    }
}