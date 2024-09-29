using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine.UIElements;

namespace Metamatic.EditorTools
{
    public abstract class AsyncEditor: Editor
    {
        private CancellationSignalSource m_EnableCancellationSignalSource;
        private Observable<bool> m_InspectorGUIIsCreated = new(false);
        protected void OnEnable()
        {
            m_EnableCancellationSignalSource = CancellationSignalSource.Create();
            RunEnableAsync().Forget();
        }

        protected void OnDisable()
        {
            m_EnableCancellationSignalSource.TryCancel();
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
            Console.Debug("AsyncEditor", "CreateInspectorGUI");
            var root =  CreateInspectorGUIAsync();
            m_InspectorGUIIsCreated.Value = true;
            return root;
        }
        
        protected abstract VisualElement CreateInspectorGUIAsync();
        protected abstract UniTask OnEnableAsync(CancellationSignal cancellationSignal);
    }
}