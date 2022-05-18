using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Metamatic {
    public class PluggableBehaviourEditorComponent: EditorComponent {
        readonly string[] optionalPropertyNames = new string[] {
            "presetName"
        };
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement() {
                name = "AutomationScriptsPlayer"
            };
            
            CreatePresetGUI(root);
            CreatePluginsGUI(root);
            CreateDefaultGUI(root);

            
            return root;
        }

        T GetReflectedPropertyValue<T>(UnityEngine.Object target, string propertyName) {
            Type targetType = target.GetType();
            PropertyInfo propertyInfo = targetType.GetProperty(propertyName);
            if (propertyInfo != null) {
                return (T)propertyInfo.GetValue(target);
            }

            return default(T);
        }

        void SetBackground(VisualElement element, Texture2D texture) {
            StyleBackground styleBackground = new StyleBackground();
            Background background = new Background();
            background.texture = texture;
            styleBackground.value = background;
            element.style.backgroundImage = styleBackground;
            element.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
        }

        void CreatePresetGUI(VisualElement root) {
            PluggableBehaviour target = (PluggableBehaviour)this.target;
            SerializedObject serializedObject = new SerializedObject(this.target);
            serializedObject.Update();

            /* Preset header */
            Label header = new Label("Preset");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = new StyleLength(10f);
            root.Add(header);

            /* Create preset menu */
            Type presetBaseType = GetReflectedPropertyValue<Type>(target, "presetBaseType");
            List<KeyValuePair<string, Type>> presetTypeNames = GetDerivedClassTypes(presetBaseType);

            List<string> values = presetTypeNames.Select(item => item.Key).ToList();
            values = values.Prepend("None").ToList();
            var presetField = new PopupField<string>(values, 0);
            root.Add(presetField);
            string currentValue = serializedObject.FindProperty("presetName").stringValue;
            string currentShortValue = "None";
            if (currentValue != "") {
                 foreach(var item in presetTypeNames) {
                    if (item.Value.FullName == currentValue) {
                        currentShortValue = item.Key;
                    }
                }
            }

            presetField.value = currentShortValue;
            
            presetField.RegisterValueChangedCallback((ChangeEvent<string> eventChange) => {
                serializedObject.Update();
                string newValue = "";
                if (eventChange.newValue != "None") {
                    foreach(var item in presetTypeNames) {
                        if (item.Key == eventChange.newValue) {
                            newValue = item.Value.FullName;
                        }
                    }
                }
                
                serializedObject.FindProperty("presetName").stringValue = newValue;
                serializedObject.ApplyModifiedProperties();
                target.OnPluggableValidate();
            });
        }

        List<KeyValuePair<string, Type>> GetDerivedClassTypes<T>() {
            return Assembly.GetAssembly(typeof(T)).GetTypes()
            .Where(type => type.IsClass && type.IsSubclassOf(typeof(T)))
            .Select<Type, KeyValuePair<string, Type>>(type => new KeyValuePair<string, Type>(type.FullName.Substring(typeof(T).FullName.Length), (Type)type))
            .ToList();
        }

        List<KeyValuePair<string, Type>> GetDerivedClassTypes(Type baseType) {
            Type[] types = Assembly.GetAssembly(baseType).GetTypes();
            return types.Where(type => type.IsClass && type.IsSubclassOf(baseType))
            .Select<Type, KeyValuePair<string, Type>>(type => {
                return new KeyValuePair<string, Type>(type.FullName.Substring(baseType.FullName.Length), (Type)type);
            })
            .ToList();
        }

        void CreatePluginsGUI(VisualElement root) {
            PluggableBehaviour target = (PluggableBehaviour)this.target;
            VisualElement container = new VisualElement();
            container.style.paddingTop = new StyleLength(10f);
            container.style.paddingBottom = new StyleLength(10f);
            root.Add(container);

            Action Repaint = null;
            Repaint = () => {
                target.OnPluggableValidate();
                CreatePluginsGUIContent(container, Repaint);
            };
            Repaint();
        }

        void CreatePluginsGUIContent(VisualElement container, Action Repaint) {
            PluggableBehaviour target = (PluggableBehaviour)this.target;
            container.Clear();
            /* Add main label */
            Label containerHeader = new Label("Plugins");
            containerHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            containerHeader.style.marginBottom = new StyleLength(10f);
            container.Add(containerHeader);

            SerializedObject serializedObject = new SerializedObject(this.target);

            /* Add plugins list */
            VisualElement pluginsContainer = new VisualElement();
            pluginsContainer.style.marginBottom = new StyleLength(6f);
            container.Add(pluginsContainer);

            /* Get class types */
            List<KeyValuePair<string, Type>> pluginTypeNames = GetDerivedClassTypes(GetReflectedPropertyValue<Type>(target, "pluginBaseType"));

            Button button = null;
            
            pluginsContainer.Clear();
            if (target.pluginNames.Length > 0) {
                foreach(string pluginTypeName in target.pluginNames) {
                    Box pluginContriner = new Box();
                    pluginContriner.style.alignItems = UnityEngine.UIElements.Align.Center;
                    pluginContriner.style.flexDirection = FlexDirection.Row;
                    pluginContriner.style.paddingTop = new StyleLength(2f);
                    pluginContriner.style.paddingLeft = new StyleLength(4f);
                    pluginContriner.style.paddingRight = new StyleLength(4f);
                    pluginContriner.style.paddingBottom = new StyleLength(2f);
                    pluginContriner.style.marginBottom = new StyleLength(2f);
                    pluginsContainer.Add(pluginContriner);
                    Label pluginLabel = new Label(pluginTypeName);
                    pluginLabel.style.flexGrow = new StyleFloat(4f);
                    pluginContriner.Add(pluginLabel);
                    Button removePLuginButton = new Button(() => {
                        SerializedProperty plugins = serializedObject.FindProperty("pluginNames");
                        serializedObject.Update();
                        int indexToRemove = Array.IndexOf(target.pluginNames, pluginTypeName);
                        plugins.DeleteArrayElementAtIndex(indexToRemove);
                        serializedObject.ApplyModifiedProperties();
                        Repaint();
                    }) {
                        text = "Remove"
                    };
                    pluginContriner.Add(removePLuginButton);
                }
            } else {
                Box noPluginsContainer = new Box();
                pluginsContainer.Add(noPluginsContainer);
                noPluginsContainer.style.paddingTop = new StyleLength(10f);
                noPluginsContainer.style.paddingLeft = new StyleLength(10f);
                noPluginsContainer.style.paddingRight = new StyleLength(10f);
                noPluginsContainer.style.paddingBottom = new StyleLength(10f);
                Label pluginLabel = new Label("No plugins connected");
                noPluginsContainer.Add(pluginLabel);
            }
                

            /* Create add button */
            VisualElement panel = null;
            button = new Button(() => {
                button.SetEnabled(false);
                panel.style.display = DisplayStyle.Flex;
            }) { text = "Add plugin" };
            button.SetEnabled(target.pluginNames.Length != pluginTypeNames.Count);
            container.Add(button);

            button.SetEnabled(target.pluginNames.Length != pluginTypeNames.Count);

            /* Add plugins selector */
            panel = new Box() as VisualElement;
            panel.style.marginBottom = new StyleLength(10f);
            panel.style.paddingTop = new StyleLength(10f);
            panel.style.paddingLeft = new StyleLength(10f);
            panel.style.paddingRight = new StyleLength(10f);
            panel.style.paddingBottom = new StyleLength(10f);
            panel.style.display = DisplayStyle.None;
            container.Add(panel);

            /* Add header */
            Label header = new Label("Adding plugin");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = new StyleLength(10f);
            panel.Add(header);

            List<string> choices = pluginTypeNames
                .Where(item => !target.pluginNames.Contains(item.Value.FullName))
                .Select(item => item.Key)
                .ToList();
            choices = choices.Prepend("").ToList();

            string selectedValue = "";
            Button acceptButton = null;
            PopupField<string> selectPluginField = new PopupField<string>(choices, 0);
            selectPluginField.style.marginBottom = new StyleLength(10f);
            panel.Add(selectPluginField);
            selectPluginField.RegisterValueChangedCallback((ChangeEvent<string> eventChange) => {
                selectedValue = eventChange.newValue;
                acceptButton.SetEnabled(selectedValue.Length != 0);
            });

            /* Add accept buttons */
            VisualElement buttonsContainer = new VisualElement();
            buttonsContainer.style.flexDirection = FlexDirection.Row;
            panel.Add(buttonsContainer);
            VisualElement emptyElementAtLeft = new VisualElement();
            emptyElementAtLeft.style.flexGrow = new StyleFloat(2f);
            Button cancelButton = new Button(() => {
                button.SetEnabled(true);
                panel.style.display = DisplayStyle.None;
            }) { text = "Cancel" };
            cancelButton.style.flexGrow = new StyleFloat(1f);

            /* Accept selecting new plugin */
            acceptButton = new Button(() => {
                SerializedProperty plugins = serializedObject.FindProperty("pluginNames");
                string fullNameValue = "";
                foreach(var item in pluginTypeNames) {
                    if (item.Key == selectedValue) {
                        fullNameValue = item.Value.FullName;
                    }
                }
                int index = Array.IndexOf<string>(target.pluginNames, fullNameValue);
                if (index < 0) {
                    serializedObject.Update();
                    int nextIndex = plugins.arraySize;
                    plugins.InsertArrayElementAtIndex(plugins.arraySize);
                    SerializedProperty newPlugin = plugins.GetArrayElementAtIndex(nextIndex);
                    newPlugin.stringValue = fullNameValue;
                    serializedObject.ApplyModifiedProperties();
                    Repaint();
                }
            }) { text = "Accept" };
            acceptButton.style.flexGrow = new StyleFloat(1f);
            acceptButton.SetEnabled(false);
            buttonsContainer.Add(emptyElementAtLeft);
            buttonsContainer.Add(cancelButton);
            buttonsContainer.Add(acceptButton);

            panel.Add(buttonsContainer);
        }

        void CreateDefaultGUI(VisualElement root) {
            SerializedObject serializedObject = new SerializedObject(this.target);
            /* Render other serialazible fields */
            SerializedProperty prop = serializedObject.GetIterator();
            if (prop.NextVisible(true)) {
                do {
                    if (prop.name != "m_Script" && !prop.name.StartsWith("_UNITY_EDITOR") && prop.name != "presetName") {
                        PropertyField propertyField = new PropertyField();
                        propertyField.label = prop.name;
                        propertyField.Bind(serializedObject);
                        propertyField.bindingPath = prop.name;

                        root.Add(propertyField);
                    }
                } while (prop.NextVisible(false));
            }
        }

        public PluggableBehaviourEditorComponent(ComposableEditor composableEditor) : base(composableEditor)
        {
        }
    }
}