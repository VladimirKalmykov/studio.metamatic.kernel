using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Metamatic.EditorTools
{
    public abstract class AsyncEditorWindow: EditorWindow
    {
        private CancellationTokenSource m_LifetimeCancellationTokenSource;
        private CancellationTokenSource m_EnableCancellationTokenSource;
        private MicroState<bool> m_IsInitialized = new MicroState<bool>(false);
        private MicroState<bool> m_IsRendered = new MicroState<bool>(false);
        
        public CancellationTokenSource lifetimeCancellationTokenSource
        {
            get
            {
                if (m_LifetimeCancellationTokenSource == null)
                {
                    m_LifetimeCancellationTokenSource = new CancellationTokenSource();
                }

                return m_LifetimeCancellationTokenSource;
            }
        }
        public CancellationTokenSource enableCancellationTokenSource
        {
            get
            {
                if (m_EnableCancellationTokenSource == null)
                {
                    m_EnableCancellationTokenSource = new CancellationTokenSource();
                }

                return m_EnableCancellationTokenSource;
            }
        }
        
        private void Awake()
        {
            InitializeAsync(InitializationReady, lifetimeCancellationTokenSource.Token).Forget();
        }

        public void CreateGUI()
        {
            CreateGUIAsync(CreateGuiReady, enableCancellationTokenSource.Token).Forget();
        }

        private void OnDisable()
        {
            if (m_LifetimeCancellationTokenSource != null)
            {
                m_LifetimeCancellationTokenSource.Cancel();
                m_LifetimeCancellationTokenSource.Dispose();
                m_LifetimeCancellationTokenSource = null;
            }
        }

        private void OnDestroy()
        {
            if (m_LifetimeCancellationTokenSource != null)
            {
                m_LifetimeCancellationTokenSource.Cancel();
                m_LifetimeCancellationTokenSource.Dispose();
                m_LifetimeCancellationTokenSource = null;
            }
        }

        private CancellationTokenAwaitable InitializationReady()
        {
            m_IsInitialized.value = true;
            return new CancellationTokenAwaitable(lifetimeCancellationTokenSource.Token);
        }
        
        private CancellationTokenAwaitable CreateGuiReady()
        {
            m_IsRendered.value = true;
            return new CancellationTokenAwaitable(enableCancellationTokenSource.Token);
        }
        
        protected abstract UniTaskVoid InitializeAsync(Func<CancellationTokenAwaitable> ready, CancellationToken cancellationToken);
        protected abstract UniTaskVoid CreateGUIAsync(Func<CancellationTokenAwaitable> ready, CancellationToken cancellationToken);
    }
}