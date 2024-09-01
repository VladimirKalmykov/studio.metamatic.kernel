using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Metamatic.EditorInspector;
using Metamatic.EditorTools;
using UnityEditor;

namespace Metamatic.EditorInspector
{
    public class ConsoleEditor: AsyncEditorWindow
    {
        [MenuItem ("Window/Metamatic/Console")]
        public static void ShowWindow () {
            GetWindow(typeof(ConsoleEditor), false, "Metamatic Console");
        }
        
        private ConsolePanelVisualElement m_ConsolePanelVisualElement;

        protected override async UniTaskVoid InitializeAsync(Func<CancellationTokenAwaitable> ready, CancellationToken cancellationToken)
        {
            await ready();
        }

        protected override async UniTaskVoid CreateGUIAsync(Func<CancellationTokenAwaitable> ready, CancellationToken cancellationToken)
        {
            Console.Debug("CreateGUIAsync");
            m_ConsolePanelVisualElement = new ConsolePanelVisualElement(cancellationToken);
            rootVisualElement.Add(m_ConsolePanelVisualElement);

            await ready();
        }
    }
}