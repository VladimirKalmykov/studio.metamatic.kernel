using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine.UIElements;

namespace Metamatic.EditorTools
{
    public abstract class AsyncOdinEditor: OdinEditor
    {
        private SignalSource m_EnableCancellationSignalSource;
        private Observable<bool> m_InspectorGUIIsCreated = new(false);
        protected override void OnEnable()
        {
            base.OnEnable();
            m_EnableCancellationSignalSource = SignalSource.Create();
            RunEnableAsync().Forget();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            m_EnableCancellationSignalSource.TryRaise();
        }
        
        protected UniTask WhenInspectorGUIIsCreated()
        {
            return m_InspectorGUIIsCreated.WaitUntilValueIs(true, m_EnableCancellationSignalSource.Signal);
        }

        private async UniTaskVoid RunEnableAsync()
        {
            try
            {
                await OnEnableAsync(m_EnableCancellationSignalSource.Signal);
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }catch (System.Exception e)
            {
                Console.Error("AsyncOdinEditor", e.Message, e.StackTrace);
            }
        }

        public sealed override VisualElement CreateInspectorGUI()
        {
            var root =  CreateInspectorGUIAsync();
            m_InspectorGUIIsCreated.Value = true;
            return root;
        }
        
        protected abstract VisualElement CreateInspectorGUIAsync();
        protected abstract UniTask OnEnableAsync(Signal cancellationSignal);
    }
}