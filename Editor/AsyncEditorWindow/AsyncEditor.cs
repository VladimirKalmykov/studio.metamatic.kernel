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
        protected void OnEnable()
        {
            m_EnableCancellationSignalSource = CancellationSignalSource.Create();
            RunEnableAsync().Forget();
        }

        protected void OnDisable()
        {
            m_EnableCancellationSignalSource.TryCancel();
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
        
        protected abstract UniTask OnEnableAsync(CancellationSignal cancellationSignal);
    }
}