using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
/* THIS IS A EXPERIMETNTAL COMPONENY */
using UnityEditor.UIElements;

namespace Metamatic {
    [Serializable]
    public class JsonAsset: ScriptableObject {
        [SerializeField]
        public string data;
    }
    public class SaveComponentToAssetsEditorComponent: EditorComponent {
        public SaveComponentToAssetsEditorComponent(ComposableEditor composableEditor) : base(composableEditor)
        {
            
        }
        public override VisualElement CreateInspectorGUI() {
            VisualElement root = new VisualElement();
            SerializedObject serializedObject = new SerializedObject(this.target);

            Button saveButton = new Button(SaveToAssets) {
                text = "Save to assets"
            };
            root.Add(saveButton);

            Button loadButton = new Button(SaveToAssets) {
                text = "Load from assets"
            };
            root.Add(loadButton);

            return root;
        }

        void SaveToAssets() {
            string fileName = $"{this.target.name}.{this.target.GetType().Name}";
            string path = EditorUtility.SaveFilePanelInProject("Save component", $"{fileName}.asset", "asset",
            "Enter a file name to save component");
            if (path.Length != 0)
            {
                JsonAsset asset = ScriptableObject.CreateInstance<JsonAsset>();
                asset.data = JsonUtility.ToJson(this.target);
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
            }
        }

        
    }
}

/// AssetDatabase.CreateAsset(this, "Assets/TestObject.asset");