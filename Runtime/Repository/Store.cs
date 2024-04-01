#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public class Store
    {
        
    }
    public class Store<T>: Store, IDisposable
    where T: struct
    {
        private InternalStore<T> m_InternalStore;
        private List<Action> m_Subscriptions = new List<Action>();
        private bool m_IsDisposed;
        public T value
        {
            get => m_InternalStore.value;
            set => m_InternalStore.value = value;
        }

        public T initialValue
        {
            get => m_InternalStore.value;
        }
        public InternalStore.DataOrigin dataOrigin => m_InternalStore.dataOrigin;
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
            if (m_IsDisposed)
            {
                return;
            }
            m_IsDisposed = true;
            m_Subscriptions.ForEach(action => action.Invoke());
            m_InternalStore.Release();
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Lock store
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public UniTask<IDisposable> Lock(CancellationToken cancellationToken = default)
        {
            return m_InternalStore.Lock(cancellationToken);
        }
        
        /// <summary>
        /// Subscribe to store changes.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public Action Subscribe(Action<T> action)
        {
            Debug.Assert(m_IsDisposed == false, "Store is disposed");
            var unsubscribe = m_InternalStore.Subscribe(action);
            m_Subscriptions.Add(unsubscribe);

            return () =>
            {
                m_Subscriptions.Remove(unsubscribe);
                unsubscribe();
            };
        }
        
        /// <summary>
        /// Subscribe to store changes.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public Action Subscribe(Action<T, InternalStore.DataStatus> action)
        {
            Debug.Assert(m_IsDisposed == false, "Store is disposed");
            var unsubscribe = m_InternalStore.Subscribe((val) =>
            {
                action(val, m_InternalStore.GetDataStatus());
            });
            m_Subscriptions.Add(unsubscribe);

            return () =>
            {
                m_Subscriptions.Remove(unsubscribe);
                unsubscribe();
            };
        }
        
        /// <summary>
        /// Save store changes to the repository.
        /// </summary>
        public async UniTask Save()
        {
            await m_InternalStore.Push();
        }

        public async UniTask CommitAllAndPush(Func<T, T> modifier)
        {
            await m_InternalStore.CommitAllAndPush(modifier);
        }

        public UniTask WhenSynced()
        {
            return m_InternalStore.WhenSynced();
        }
    }
}