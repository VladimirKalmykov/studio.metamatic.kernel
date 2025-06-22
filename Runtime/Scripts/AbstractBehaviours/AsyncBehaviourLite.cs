using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public abstract class AsyncBehaviourLite: MonoBehaviour
    {
        private SignalSource m_DisableSignalSource = default;

        private void OnEnable()
        {
            RunEnableAsync().Forget();
        }

        private async UniTaskVoid RunEnableAsync()
        {
            try
            {
                m_DisableSignalSource.TryRaise();
                m_DisableSignalSource = SignalSource.Create();
                await OnEnableAsync(m_DisableSignalSource.Signal);
            }
            catch (OperationCanceledException)
            {
                // Skipping
            }
            catch (Exception ex)
            {
                Console.Error("AsyncBehaviourLite", this.GetType().Name, ex);
            }
        }


        protected abstract UniTask OnEnableAsync(Signal disableSignal);
    }
}