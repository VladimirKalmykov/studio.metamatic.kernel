namespace Metamatic
{
    using System.Threading;
    using UnityEngine;
    using UnityEngine.InputSystem;

    public class TerminalService : Service
    {
        [SerializeField] UITerminal m_Terminal;
        
        public UITerminal terminal => m_Terminal;

        protected override async void RunService(CancellationToken cancellationToken)
        {
            #if DEBUG
            m_Terminal.gameObject.SetActive(true);
            #endif
            await cancellationToken;
        }

        private void Update()
        {
            /* Listen for press TAB key on keyboard using Input System */
            if (Keyboard.current.tabKey.wasPressedThisFrame)
            {
                m_Terminal.gameObject.SetActive(!m_Terminal.gameObject.activeSelf);
            }
        }

        public UITerminal.TextRow AddTextRow(string text)
        {
            return m_Terminal.AddTextRow(text);
        }
    }

}