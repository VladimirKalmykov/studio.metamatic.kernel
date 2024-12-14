using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public class ObservableMonoBehaviour: MonoBehaviour
    {
        public event Action OnDisableEvent;
        public event Action OnDestroyEvent;
        
        private int m_Version;

        private void OnDisable()
        {
            OnDisableEvent?.Invoke();
            m_Version++;
        }
        
        private void OnDestroy()
        {
            OnDestroyEvent?.Invoke();
        }
        
        public DisposableAction AddOnDisableListenerOnce(Action action)
        {
            var version = m_Version;
            void Handler()
            {
                OnDisableEvent -= Handler;
                if (m_Version == version)
                {
                    action();
                }
            }
            OnDisableEvent += Handler;
            return new DisposableAction(() =>
            {
                if (this == null || this.Equals(null)) return;
                OnDisableEvent -= Handler;
            });
        }
        
        public DisposableAction AddOnDestroyListenerOnce(Action action)
        {
            var version = m_Version;
            void Handler()
            {
                OnDestroyEvent -= Handler;
                if (m_Version == version)
                {
                    action();
                }
            }
            OnDestroyEvent += Handler;
            return new DisposableAction(() =>
            {
                if (this == null || this.Equals(null)) return;
                OnDestroyEvent -= Handler;
            });
        }

        public async UniTask WhenOnDestroy(CancellationSignal cancellationSignal)
        {
            var tcs = SafeUniTaskCompletionSource.Create();
            using var listening = AddOnDestroyListenerOnce(() => tcs.TrySetResult());
            using var listenForSignal = cancellationSignal.Register(() => tcs.TrySetCanceled());
            await tcs.Task;
        } 
    }
}