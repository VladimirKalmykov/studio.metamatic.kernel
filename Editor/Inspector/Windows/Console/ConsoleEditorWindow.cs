using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Metamatic.EditorInspector;
using Metamatic.EditorTools;
using UnityEditor;

namespace Metamatic.EditorInspector
{
    public class ConsoleEditorWindow: AsyncEditorWindow
    {
        [MenuItem ("Window/Metamatic/Console")]
        public static void ShowWindow () {
            GetWindow(typeof(ConsoleEditorWindow), false, "Metamatic Console");
        }
        
        private ConsolePanelVisualElement m_ConsolePanelVisualElement;

        protected override async UniTaskVoid InitializeAsync(Func<CancellationTokenAwaitable> ready, CancellationSignal signal)
        {
            await ready();
        }

        protected override async UniTaskVoid CreateGUIAsync(Func<CancellationTokenAwaitable> ready, CancellationSignal signal)
        {
            m_ConsolePanelVisualElement = new ConsolePanelVisualElement(signal);
            rootVisualElement.Add(m_ConsolePanelVisualElement);

            await ready();
        }
    }
}