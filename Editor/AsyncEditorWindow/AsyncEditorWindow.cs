using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Metamatic.EditorTools
{
    public abstract class AsyncEditorWindow: EditorWindow
    {
        private CancellationSignalSource m_LifetimeCancellationTokenSource;
        private CancellationSignalSource m_EnableCancellationSignalSource;
        private Observable<bool> m_IsReady = new();
        private bool m_GUIReady;
        private bool m_Enabled;
        private event Action OnGUIReady;
        
        public CancellationSignal lifetimeCancellationSignal => m_LifetimeCancellationTokenSource.Signal;
        public CancellationSignal enableCancellationSignal => m_EnableCancellationSignalSource.Signal;
        
        private void Awake()
        {
            m_LifetimeCancellationTokenSource = CancellationSignalSource.Create();
            AwakeAsync(lifetimeCancellationSignal).Forget();
        }

        private void OnEnable()
        {
            m_EnableCancellationSignalSource = CancellationSignalSource.Create();
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
            m_EnableCancellationSignalSource.TryCancel();
        }

        private void OnDestroy()
        {
            m_LifetimeCancellationTokenSource.TryCancel();
        }
        
        private UniTask SetEditorReady()
        {
            m_IsReady.value = true;
            return enableCancellationSignal.ToUniTask();
        }

        protected virtual UniTaskVoid AwakeAsync(CancellationSignal signal)
        {
            return new UniTaskVoid();
        }
        protected abstract UniTaskVoid EnableAsync(Func<UniTask> ready, CancellationSignal signal);
        protected abstract void OnCreateGUI();
    }
}