using Cysharp.Threading.Tasks;
using TMPro;

namespace Metamatic
{
    using System.Threading;
    using Metamatic.Actions;

    public class DispatcherVariableToTextMeshProTextUI : AsyncBehaviour
    {
        public string variableName;
        public string fallbackText;
        private TextMeshProUGUI m_TextMeshProUGUI;

        protected override async UniTask Run(Ready ready, CancellationToken cancellationToken)
        {
            m_TextMeshProUGUI = GetComponent<TextMeshProUGUI>();
            using var listener = Dispatcher.global.SubscribeVariable<string>(variableName, OnValueChange, fallbackText);
            await ready();
        }

        private void OnValueChange(string value)
        {
            m_TextMeshProUGUI.text = value;
        }
    }

}