using UnityEngine.UI;

namespace Metamatic.UI {
    using System;
    using UnityEngine;
    using Sirenix.OdinInspector;

    /// <summary>
    /// One solution to set TextMeshPro and Text text programmatically
    /// </summary>
    [Obsolete("Component has performance issues", true)]
    public class TextValue: MonoBehaviour {
        [Multiline(3)]
        public string text = "";
        public string format = "{0}";
        Text textMesh;
        UnityEngine.UI.Text uguiText;
        private System.Action OnUpdate;
        
        void Start() {
            uguiText = GetComponent<UnityEngine.UI.Text>();
            textMesh = GetComponent<Text>();

            if (uguiText != null) {
                OnUpdate = SetUGUI;
            } else if (textMesh != null) {
                OnUpdate = SetTextMesh;
            } else {
                OnUpdate = DoNothing;
            }
        }

        void Update() {
            OnUpdate.Invoke();
        }

        void SetTextMesh() {
            textMesh.text = String.Format(format, text);
        }

        void SetUGUI() {
            uguiText.text = String.Format(format, text);
        }

        void DoNothing() {

        }

        public void SetText(string value) {
            text = value;
        }
    }
}