using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Effects
{
    public abstract class VisualEffect<T>: MonoBehaviour
    where T: struct
    {
        private CancellationSignalSource m_DisableSignalSource;
        private DisposableAction m_EffectSubscription;
        private bool m_IsReady;
        private event Action OnReady;
        private void OnEnable()
        {
            m_DisableSignalSource = CancellationSignalSource.Create();
            m_EffectSubscription = Effect<T>.SubscribeAll(OnInternalEffectCreated);
            OnVisualEffectEnabled(InternalReady, m_DisableSignalSource.Signal).Forget();
        }

        private void OnDisable()
        {
            m_IsReady = false;
            m_DisableSignalSource.TryCancel();
        }

        private UniTask InternalReady()
        {
            m_IsReady = true;
            OnReady?.Invoke();
            return m_DisableSignalSource.Signal.ToUniTask();
        }
        
        private void OnInternalEffectCreated(Effect<T> effect)
        {
            if (m_IsReady)
            {
                OnEffect(effect).Forget();
            }
            else
            {
                if (effect.instant || effect.cancellationSignal.IsCancellationRequested)
                {
                    return;
                }
                DisposableAction registration = default;
                void OnReadyHandler()
                {
                    registration.Dispose();
                    OnEffect(effect).Forget();
                }
                OnReady += OnReadyHandler;
                registration = effect.cancellationSignal.Register(() => OnReady -= OnReadyHandler);
            }
        }
        
        protected abstract UniTaskVoid OnVisualEffectEnabled(Func<UniTask> ready, CancellationSignal cancellationSignal);
        protected abstract UniTaskVoid OnEffect(Effect<T> effect);
    }
}