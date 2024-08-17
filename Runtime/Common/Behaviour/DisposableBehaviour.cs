using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    /// <summary>
    /// This class gives you the ability to add async disposal actions and dispose listeners to a MonoBehaviour.
    /// Important: this class do not dispose by itself, you need to call DisposeAsync or Dispose manually, thus it is not suitable for using as a disable/destroy listener.
    /// </summary>
    public class DisposableBehaviour: MonoBehaviour, IUniTaskAsyncDisposable, IDisposable, IExposedDisposed
    {
        private int m_Version;
        private readonly List<Func<UniTask>> m_AsyncDisposalActions = new List<Func<UniTask>>();
        
        public DisposableAction AddAsyncDisposalAction(Func<UniTask> action)
        {
            var version = m_Version;
            m_AsyncDisposalActions.Add(action);
            return new DisposableAction(() =>
            {
                if (m_Version != version) return;
                m_AsyncDisposalActions.Remove(action);
            });
        }

        public DisposableAction AddDisposeListenerOnce(Action action)
        {
            var version = m_Version;
            OnDisposed += action;
            return new DisposableAction(() =>
            {
                if (m_Version != version) return;
                OnDisposed -= action;
            });
        }
        
        public async UniTask DisposeAsync()
        {
            Dispose(true);
            isAsyncDisposed = true;
            OnAsyncDisposed?.Invoke();
            if (m_AsyncDisposalActions.Count == 0)
            {
                return;
            }

            try
            {
                await UniTask.WhenAll(m_AsyncDisposalActions.Select(action => action()));
            } catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
        
        public void Dispose()
        {
            Dispose(false);
        }

        private void Dispose(bool isFromAsync)
        {
            if (isDisposed)
            {
                return;
            }
            OnDisposed?.Invoke();
            isDisposed = true;
            if (!isFromAsync)
            {
                DisposeAsync().Forget();
                return;
            }
        }

        public bool isDisposed { get; private set; }
        public bool isAsyncDisposed { get; private set; }
        public event Action OnDisposed;
        public event Action OnAsyncDisposed;
    }
}