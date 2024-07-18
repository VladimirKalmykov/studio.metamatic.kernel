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

        protected override async void RunAsync(CancellationToken cancellationToken)
        {
            m_TextMeshProUGUI = GetComponent<TextMeshProUGUI>();
            using var listener = Dispatcher.global.SubscribeVariable<string>(variableName, OnValueChange, fallbackText);
            await cancellationToken;
        }

        private void OnValueChange(string value)
        {
            m_TextMeshProUGUI.text = value;
        }
    }

}