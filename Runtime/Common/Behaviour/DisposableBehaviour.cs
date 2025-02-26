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
    public class DisposableBehaviour: MonoBehaviour, IAsyncDisposableAbortable, IExposedDisposedAsync
    {
        private int m_Version;
        private readonly List<Func<UniTask>> m_AsyncDisposalActions = new List<Func<UniTask>>();
        public event Action OnDestroyEvent;
        
        #region Mono

        private void OnDestroy()
        {
            OnDestroyEvent?.Invoke();
            OnDestroyEvent = null;
        }

        #endregion
        
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

        public DisposableAction AddAbortListenerOnce(Action action)
        {
            var version = m_Version;

            void Handler()
            {
                OnAbort -= Handler;
                action();
            }
            OnAbort += Handler;
            return new DisposableAction(() =>
            {
                if (this.Equals(null) || m_Version != version) return;
                OnAbort -= Handler;
            });
        }
        
        public async UniTask DisposeAsync()
        {
            Dispose(false);

            try
            {
                await UniTask.WhenAll(m_AsyncDisposalActions.Select(action => action()));
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                isDisposedAsync = true;
                OnAsyncDisposed?.Invoke(e);
                OnAsyncDisposed = null;
            }
            finally
            {
                if (!isDisposedAsync)
                {
                    OnAsyncDisposed?.Invoke(null);
                    OnAsyncDisposed = null;
                }
                isAborted = true;
                OnAbort?.Invoke();
                OnAbort = null;
            }
        }

        private void Dispose(bool isAbort)
        {
            if (isAborted)
            {
                return;
            }
            
            if (isAbort)
            {
                DisposeAsync().Forget();
                Abort();
            }
        }

        public void Reset()
        {
            unchecked
            {
                m_Version++;
            }
            isAborted = false;
            isDisposedAsync = false;
            OnAbort = null;
            OnAsyncDisposed = null;
        }

        public bool isAborted { get; private set; }
        public bool isDisposedAsync { get; private set; }
        public event Action OnAbort;
        public event Action<Exception> OnAsyncDisposed;
        public void Abort()
        {
            OnAbort?.Invoke();
            OnAbort = null;
            Dispose(true);
        }

        public DisposableAction AddAbortedListenerOnce(Action action)
        {
            var version = m_Version;
            void Handler()
            {
                OnAbort -= Handler;
                action();
            }
            OnAbort += Handler;
            return new DisposableAction(() =>
            {
                if (this.Equals(null) || m_Version != version) return;
                OnAbort -= Handler;
            });
        }
        
        public event Action<Exception> OnDisposedAsync;
    }
}