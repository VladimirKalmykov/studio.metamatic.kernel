namespace Metamatic
{
    using System;
    using System.Threading;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    public class UITerminal : AsyncBehaviour
    {
        public class Row : IDisposable
        {
            protected GameObject m_Row;
            protected bool m_Persisted;
            protected bool m_Disposed;
            public GameObject element => m_Row;

            public Row(UITerminal terminal)
            {
                m_Row = Instantiate(terminal.m_RowPrefab, terminal.m_ContentComponent.transform);
            }

            public void Persist()
            {
                m_Persisted = true;
                Dispose();
            }

            public virtual void Dispose()
            {
                if (m_Disposed) return;
                m_Disposed = true;
                if (!m_Persisted)
                {
                    Destroy(m_Row);
                }
                m_Row = null;
            }
        }

        public class TextRow : Row
        {
            private TMP_Text m_TextComponent;

            public string text
            {
                set => m_TextComponent.text = value;
            }

            public TextRow(UITerminal terminal, string text) : base(terminal)
            {
                m_TextComponent = Instantiate(terminal.m_TextPrefab, m_Row.transform);
                m_TextComponent.text = text;
                m_TextComponent.font = terminal.m_FontAsset;
                m_TextComponent.fontSize = terminal.m_FontSize;
            }

            public void SetText(string text)
            {
                if (m_Disposed) return;
                m_TextComponent.text = text;
            }

            public override void Dispose()
            {
                if (m_Disposed) return;
                if (!m_Persisted)
                {
                    Destroy(m_TextComponent.gameObject);
                }
                m_TextComponent = null;
                base.Dispose();
            }
        }

        [SerializeField] private ScrollRect m_ScrollRect;
        [SerializeField] private GameObject m_ContentComponent;
        [SerializeField] private TMP_InputField m_InputField;
        [SerializeField] private GameObject m_RowPrefab;
        [SerializeField] private TMP_Text m_TextPrefab;
        [SerializeField] private TMP_FontAsset m_FontAsset;
        [SerializeField] private float m_FontSize;
        
        public event Action<string> OnCommand;

        protected override async void RunAsync(CancellationToken cancellationToken)
        {
            /* Subscribe to input press enter event */
            m_InputField.onSubmit.AddListener(OnSubmit);
            await cancellationToken;
            m_InputField.onSubmit.RemoveListener(OnSubmit);
        }
        
        private void OnSubmit(string text)
        {
            OnCommand?.Invoke(text);
            m_InputField.text = string.Empty;
            /* Place focus back to input field */
            m_InputField.ActivateInputField();
        }

        public Row AddRow()
        {
            return new Row(this);
        }

        public TextRow AddTextRow(string text)
        {
            return new TextRow(this, text);
        }
    }

}