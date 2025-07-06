using Cysharp.Threading.Tasks;
using Metamatic;

namespace Metamatic {
    using System;
    using UnityEngine;

    /// <summary>
    /// This class will be added to every game object from the object pool
    /// </summary>
    [DisallowMultipleComponent]
    public class RecyclableGameObject: MonoBehaviour {
        public event Action OnRecycle;
        public void Recycle() {
            var handlers = OnRecycle;
            OnRecycle = null;
            handlers?.Invoke();
            
            this.gameObject.SetActive(false);
        }

        public UniTask WaitForRecycle(Signal cancellationSignal)
        {
            var tsc = SafeUniTaskCompletionSource.Create();
            DisposableAction registration = default;
            OnRecycle += HandleRecycle;
            
            registration = cancellationSignal.Register(HandleCancel);
            
            void HandleRecycle() {
                registration.Dispose();
                OnRecycle -= HandleRecycle;
                tsc.TrySetResult();
            }

            void HandleCancel()
            {
                OnRecycle -= HandleRecycle;
                tsc.TrySetCanceled();
            }
            
            return tsc.Task;
        }
    }
}