using UnityEngine.UIElements;

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic
{
    public abstract class AsyncVisualElement: VisualElement
    {
        private CancellationSignalSource m_DetachSignalSource;
        private bool m_IsAttachedAsync;
        private event Action OnAttachedAsyncEvent;
        public AsyncVisualElement()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }
        
        private void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            m_DetachSignalSource = CancellationSignalSource.Create();
            OnAttachedAsync(InternalReady, m_DetachSignalSource.Signal).Forget();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            m_IsAttachedAsync = false;
            OnAttachedAsyncEvent = null;
            m_DetachSignalSource.TryCancel();
        }

        private UniTask InternalReady()
        {
            m_IsAttachedAsync = true;
            OnAttachedAsyncEvent?.Invoke();
            return m_DetachSignalSource.Signal.ToUniTask();
        }

        public abstract UniTaskVoid OnAttachedAsync(Func<UniTask> ready, CancellationSignal detachSignal);
    }
}