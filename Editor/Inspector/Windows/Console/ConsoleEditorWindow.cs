using System;
using Cysharp.Threading.Tasks;
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


        protected override async UniTaskVoid EnableAsync(Func<UniTask> ready, Signal signal)
        {
            m_ConsolePanelVisualElement = new ConsolePanelVisualElement(signal);
            rootVisualElement.Add(m_ConsolePanelVisualElement);

            await ready();
        }

        protected override void OnCreateGUI()
        {
            // Ignore because we are create GUI in EnableAsync
        }
    }
}