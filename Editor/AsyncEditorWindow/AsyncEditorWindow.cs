using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Metamatic.EditorTools
{
    public abstract class AsyncEditorWindow: EditorWindow
    {
        private SignalSource m_LifetimeCancellationTokenSource;
        private SignalSource m_EnableCancellationSignalSource;
        private Observable<bool> m_IsReady = new();
        private bool m_GUIReady;
        private bool m_Enabled;
        private event Action OnGUIReady;
        
        public Signal lifetimeCancellationSignal => m_LifetimeCancellationTokenSource.Signal;
        public Signal enableCancellationSignal => m_EnableCancellationSignalSource.Signal;
        
        private void Awake()
        {
            m_LifetimeCancellationTokenSource = SignalSource.Create();
            AwakeAsync(lifetimeCancellationSignal).Forget();
        }

        private void OnEnable()
        {
            m_EnableCancellationSignalSource = SignalSource.Create();
            m_Enabled = true;
            if (m_GUIReady)
            {
                EnableAsync(SetEditorReady, enableCancellationSignal).Forget();
            }
        }

        public void CreateGUI()
        {
            OnCreateGUI();
            if (m_Enabled)
            {
                EnableAsync(SetEditorReady, enableCancellationSignal).Forget();
            }
        }

        private void OnDisable()
        {
            m_Enabled = false;
            m_EnableCancellationSignalSource.TryRaise();
        }

        private void OnDestroy()
        {
            m_LifetimeCancellationTokenSource.TryRaise();
        }
        
        private UniTask SetEditorReady()
        {
            m_IsReady.value = true;
            return enableCancellationSignal.ToUniTask();
        }

        protected virtual UniTaskVoid AwakeAsync(Signal signal)
        {
            return new UniTaskVoid();
        }
        protected abstract UniTaskVoid EnableAsync(Func<UniTask> ready, Signal signal);
        protected abstract void OnCreateGUI();
    }
}