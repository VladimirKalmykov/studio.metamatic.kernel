using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Localization;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Metamatic
{
    public class LocalizedStringSubscription: IDisposable
    {
        private readonly LocalizedString m_LocalizedString;
        private readonly LocalizedString.ChangeHandler m_OnValueChange;
        private bool m_Disposed;
        private CancellationTokenSource m_CancellationTokenSource;
        public LocalizedStringSubscription(LocalizedString localizedString, LocalizedString.ChangeHandler onValueChange)
        {
            m_CancellationTokenSource = new CancellationTokenSource();
            m_LocalizedString = localizedString;
            m_OnValueChange = onValueChange;
            localizedString.StringChanged += onValueChange;
            /*
             * We must use async operation to support WebGL platform. See https://forum.unity.com/threads/webgl-webglplayer-does-not-support-synchronous-addressable-loading.1396264/
             */
            var asyncOperation = localizedString.GetLocalizedStringAsync();
            AwaitForValue(asyncOperation);
        }

        private async void AwaitForValue(AsyncOperationHandle<string> asyncOperation)
        {
            try
            {
                await asyncOperation.WithCancellation(m_CancellationTokenSource.Token);
                m_OnValueChange.Invoke(asyncOperation.Result);
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
        }

        public void Dispose()
        {
            if (m_Disposed)
            {
                return;
            }
            m_CancellationTokenSource.Cancel();
            m_Disposed = true;
            m_LocalizedString.StringChanged -= m_OnValueChange;
        }
    }
}