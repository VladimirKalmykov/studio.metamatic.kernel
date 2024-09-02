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
        private CancellationSignalSource m_EnableCancellationTokenSource;
        private MicroState<bool> m_IsInitialized = new MicroState<bool>(false);
        private MicroState<bool> m_IsRendered = new MicroState<bool>(false);
        
        public CancellationSignalSource lifetimeCancellationTokenSource
        {
            get
            {
                if (!m_LifetimeCancellationTokenSource.CanBeCancelled)
                {
                    m_LifetimeCancellationTokenSource = CancellationSignalSource.Create();
                }

                return m_LifetimeCancellationTokenSource;
            }
        }
        public CancellationSignalSource enableCancellationTokenSource
        {
            get
            {
                if (!m_EnableCancellationTokenSource.CanBeCancelled)
                {
                    m_EnableCancellationTokenSource = CancellationSignalSource.Create();
                }

                return m_EnableCancellationTokenSource;
            }
        }
        
        private void Awake()
        {
            InitializeAsync(InitializationReady, lifetimeCancellationTokenSource.Signal).Forget();
        }

        public void CreateGUI()
        {
            CreateGUIAsync(CreateGuiReady, enableCancellationTokenSource.Signal).Forget();
        }

        private void OnDisable()
        {
            m_LifetimeCancellationTokenSource.TryCancel();
        }

        private void OnDestroy()
        {
            m_LifetimeCancellationTokenSource.TryCancel();
        }

        private CancellationTokenAwaitable InitializationReady()
        {
            m_IsInitialized.value = true;
            return new CancellationTokenAwaitable(lifetimeCancellationTokenSource.Signal);
        }
        
        private CancellationTokenAwaitable CreateGuiReady()
        {
            m_IsRendered.value = true;
            return new CancellationTokenAwaitable(enableCancellationTokenSource.Signal);
        }
        
        protected abstract UniTaskVoid InitializeAsync(Func<CancellationTokenAwaitable> ready, CancellationSignal signal);
        protected abstract UniTaskVoid CreateGUIAsync(Func<CancellationTokenAwaitable> ready, CancellationSignal signal);
    }
}