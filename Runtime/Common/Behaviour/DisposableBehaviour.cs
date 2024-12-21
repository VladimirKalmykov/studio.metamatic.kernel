using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metamatic
{
    /// <summary>
    /// This class gives you the ability to add async disposal actions and dispose listeners to a MonoBehaviour.
    /// Important: this class do not dispose by itself, you need to call DisposeAsync or Dispose manually, thus it is not suitable for using as a disable/destroy listener.
    /// </summary>
    public class DisposableBehaviour: MonoBehaviour, IUniTaskAsyncDisposable, IDisposable, IExposedDisposedSync
    {
        private int m_Version;
        private readonly List<Func<UniTask>> m_AsyncDisposalActions = new List<Func<UniTask>>();
        
        public DisposableAction AddAsyncDisposalActionOnce(Func<UniTask> action)
        {
            var version = m_Version;
            m_AsyncDisposalActions.Add(action);
            return new DisposableAction(() =>
            {
                if (this.Equals(null) || m_Version != version) return;
                m_AsyncDisposalActions.Remove(action);
            });
        }

        public DisposableAction AddAsyncDisposeListenerOnce(Action<Exception> action)
        {
            var version = m_Version;
            void Handler(Exception e)
            {
                OnAsyncDisposed -= Handler;
                action(e);
            }
            OnAsyncDisposed += Handler;
            return new DisposableAction(() =>
            {
                if (this.Equals(null) || m_Version != version) return;
                OnAsyncDisposed -= Handler;
            });
        }

        public DisposableAction AddDisposeListenerOnce(Action action)
        {
            var version = m_Version;

            void Handler()
            {
                OnDisposedSync -= Handler;
                action();
            }
            OnDisposedSync += Handler;
            return new DisposableAction(() =>
            {
                if (this.Equals(null) || m_Version != version) return;
                OnDisposedSync -= Handler;
            });
        }
        
        public async UniTask DisposeAsync()
        {
            Dispose(true);

            try
            {
                await UniTask.WhenAll(m_AsyncDisposalActions.Select(action => action()));
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                isAsyncDisposed = true;
                OnAsyncDisposed?.Invoke(e);
                OnAsyncDisposed = null;
            }
            finally
            {
                if (!isAsyncDisposed)
                {
                    OnAsyncDisposed?.Invoke(null);
                }
            }
        }
        
        [Button]
        public void Dispose()
        {
            Dispose(false);
        }

        private void Dispose(bool isFromAsync)
        {
            if (isDisposedSync)
            {
                return;
            }
            isDisposedSync = true;
            OnDisposedSync?.Invoke();
            OnDisposedSync = null;
            
            if (!isFromAsync)
            {
                DisposeAsync().Forget();
                return;
            }
        }

        public void Reset()
        {
            unchecked
            {
                m_Version++;
            }
            isDisposedSync = false;
            isAsyncDisposed = false;
            OnDisposedSync = null;
            OnAsyncDisposed = null;
        }

        public bool isDisposedSync { get; private set; }
        public bool isAsyncDisposed { get; private set; }
        public event Action OnDisposedSync;
        public event Action<Exception> OnAsyncDisposed;
    }
}