using UnityEngine.UIElements;

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic.UIElements
{
    public abstract class AsyncVisualElement: VisualElement
    {
        private SignalSource m_DetachSignalSource;
        public Signal detachSignal => m_DetachSignalSource.Signal;
        public AsyncVisualElement()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }
        
        private void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            m_DetachSignalSource = SignalSource.Create();
            RunOnAttachToPanelAsync().Forget();
        }
        
        private async UniTaskVoid RunOnAttachToPanelAsync()
        {
            try
            {
                await OnAttachedAsync(m_DetachSignalSource.Signal);
            }
            catch (Exception e)
            {
                Console.Error("AsyncVisualElement", e);
            }
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            m_DetachSignalSource.TryRaise();
        }

        public abstract UniTask OnAttachedAsync(Signal detachSignal);
    }
}