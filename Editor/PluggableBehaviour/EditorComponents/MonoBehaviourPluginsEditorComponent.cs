using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Metamatic {
    public class MonoBehaviourPluginsEditorComponent: EditorComponent {
        public class PluginItem: VisualElement {
            bool connected = false;
            public PluginItem(Type componentType, bool initialConnected, MonoBehaviour target): base() {
                this.connected = initialConnected;
                VisualElement root = new VisualElement();
                root.AddToClassList("PluginItem");
                if (connected) {
                    root.AddToClassList("selected");
                }
                
                Image image = new Image();
                image.AddToClassList("InlineIcon");
                image.image = AssetDatabase.LoadAssetAtPath<Sprite>("Packages/studio.metamatic.kernel/Editor/Sprites/checked.png").texture;
                image.style.display = initialConnected ? DisplayStyle.Flex : DisplayStyle.None;
                Label pluginLabel = new Label(componentType.Name);
                pluginLabel.style.flexGrow = new StyleFloat(4f);
                root.Add(image);
                root.Add(pluginLabel);

                var allComponents = target.GetComponents(componentType);
                Button button = connected
                    ? new Button(() => {
                        foreach(var componentToRemove in allComponents) {
                            if (Application.isPlaying) {
                                UnityEngine.Component.Destroy(componentToRemove);
                            } else {
                                UnityEngine.Object.DestroyImmediate(componentToRemove);
                            }
                        }
                        this.connected = false;
                        var onValidateMethod = target
                            .GetType()
                            .GetMethod("OnValidate", BindingFlags.NonPublic | BindingFlags.Instance);
                        
                        if (onValidateMethod != null) {
                            onValidateMethod.Invoke(target, null);
                        }
                    }) {
                        text = "Remove"
                    }
                    : new Button(() => {
                        

                        this.connected = true;
                        if (target != null) {
                            target.gameObject.AddComponent(componentType);
                            EditorSceneManager.MarkSceneDirty(target.gameObject.scene);

                            var onValidateMethod = target
                                .GetType()
                                .GetMethod("OnValidate", BindingFlags.NonPublic | BindingFlags.Instance);
                            
                            if (onValidateMethod != null) {
                                onValidateMethod.Invoke(target, null);
                            }
                        }
                    }) {
                        text = "Add"
                    };

                if (connected) {
                    button.style.backgroundColor = new StyleColor(MiscUtils.HexCodeToColor("#0F3F3D"));
                }
                root.Add(button);
                this.Add(root);
            }
        }

        readonly string[] optionalPropertyNames = new string[] {
            "presetName"
        };
        VisualElement panelContent;
        public static List<int> expandedIds = new List<int>();
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>
            ("Packages/studio.metamatic.kernel/Editor/PluggableBehaviour/EditorComponents/styles.uss");
            root.styleSheets.Add(styleSheet);
            
            CreatePluginsGUI(root);
            
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
            MonoBehaviour target = (MonoBehaviour)this.target;
            VisualElement container = new VisualElement();
            container.style.paddingTop = new StyleLength(10f);
            container.style.paddingBottom = new StyleLength(10f);
            root.Add(container);

            Action Repaint = null;
            Repaint = () => {
                CreatePluginsGUIContent(container, Repaint);
            };
            Repaint();
        }

        KeyValuePair<string, Type>[] cached_PluginsList;
        /// <summary>
        /// All aviable classes drived from monobehaviour and has attribute PluginFor
        /// </summary>
        /// <returns></returns>
        KeyValuePair<string, Type>[] GetPluginsList() {
            var thisTypes = GetParentTypes(this.target.GetType());
            var plugins = new List<KeyValuePair<string, Type>>();
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    // Debug.Log($"Type: {type.FullName}");
                    if (type.IsSubclassOf(typeof(MonoBehaviour))) {
                        var pluginForAttributes = type.GetCustomAttributes(typeof(PluginFor), true);

                        foreach(var pluginForAttribute in pluginForAttributes) {
                            foreach(var thisType in thisTypes) {
                                if (((PluginFor)pluginForAttribute).types.Contains(thisType) && !type.IsAbstract) {
                                    plugins.Add(new KeyValuePair<string, Type>(type.FullName, type));
                                }
                            }
                        }
                    }
                        
                }
            }

            return plugins.ToArray();
        }

        public static IEnumerable<Type> GetParentTypes(Type type)
        {
            // is there any base type?
            if (type == null)
            {
                yield break;
            }

            yield return type;

            // return all implemented or inherited interfaces
            foreach (var i in type.GetInterfaces())
            {
                yield return i;
            }

            // return all inherited types
            var currentBaseType = type.BaseType;
            while (currentBaseType != null)
            {
                yield return currentBaseType;
                currentBaseType= currentBaseType.BaseType;
            }
        }

        Type[] GetConnectedPlugins() {
            var thisTypes = GetParentTypes(this.target.GetType());
            var allComponents = ((MonoBehaviour)this.target).gameObject.GetComponents<MonoBehaviour>();
            List<Type> plugins = new List<Type>();
            foreach(var component in allComponents) {
                var pluginForAttributes = component.GetType().GetCustomAttributes(typeof(PluginFor), true);

                foreach(var pluginForAttribute in pluginForAttributes) {
                    foreach(var thisType in thisTypes) {
                        if (((PluginFor)pluginForAttribute).types.Contains(thisType)) {
                            plugins.Add(component.GetType());
                            continue;
                        }
                    }
                }
                
            }

            return plugins.ToArray();
        }

        void CreatePluginsGUIContent(VisualElement container, Action Repaint) {
            container.Clear();

            /* Create add button */
            VisualElement panel = null;

            /* Add plugins selector */
            panel = new VisualElement();
            panel.style.backgroundColor = new StyleColor(MiscUtils.HexCodeToColor("#404040"));
            panel.style.marginBottom = new StyleLength(2f);
            panel.style.borderTopLeftRadius = new StyleLength(3f);
            panel.style.borderTopRightRadius = new StyleLength(3f);
            panel.style.borderBottomRightRadius = new StyleLength(2f);
            panel.style.borderBottomLeftRadius = new StyleLength(2f);
            container.Add(panel);

            /* Add plugin header */
            var header = new Button(() => {
                int id = this.target.GetInstanceID();
                if (expandedIds.Contains(id)) {
                    expandedIds.Remove(id);
                } else {
                    expandedIds.Add(id);
                }
                DrawPluginsManager();
            });
            header.AddToClassList("PluginsPanelHeader");

            var icon = new Image();
            icon.AddToClassList("InlineIcon");
            icon.image = AssetDatabase.LoadAssetAtPath<Sprite>("Packages/studio.metamatic.kernel/Editor/Sprites/plugins.png").texture;

            header.Add(icon);
            header.Add(new Label("Plugins"));

            panel.Add(header);

            /* Add panel content */
            panelContent = new VisualElement();
            panelContent.style.paddingTop = new StyleLength(2);
            panelContent.style.backgroundColor = new StyleColor(MiscUtils.HexCodeToColor("#404040"));
            panelContent.style.borderTopColor = new StyleColor(MiscUtils.HexCodeToColor("#303030"));
            panelContent.style.borderRightColor = new StyleColor(MiscUtils.HexCodeToColor("#303030"));
            panelContent.style.borderBottomColor = new StyleColor(MiscUtils.HexCodeToColor("#303030"));
            panelContent.style.borderLeftColor = new StyleColor(MiscUtils.HexCodeToColor("#303030"));
            panelContent.style.borderTopWidth = 0f;
            panelContent.style.borderRightWidth = 1f;
            panelContent.style.borderBottomWidth = 1f;
            panelContent.style.borderLeftWidth = 1f;
            panelContent.style.borderBottomLeftRadius = 2f;
            panelContent.style.borderBottomRightRadius = 2f;
            panel.Add(panelContent);

            /* Add plugins list */
            DrawPluginsManager();
        }

        void DrawPluginsManager() {
            int id = this.target.GetInstanceID();
            var isVisible = expandedIds.Contains(id);
            if (isVisible) {
                var innerContainer = new VisualElement();
                innerContainer.style.paddingTop = new StyleLength(2);
                innerContainer.style.paddingLeft = new StyleLength(4f);
                innerContainer.style.paddingRight = new StyleLength(4f);
                innerContainer.style.paddingBottom = new StyleLength(4f);

                MonoBehaviour target = (MonoBehaviour)this.target;
                /* Get class types */
                KeyValuePair<string, Type>[] pluginTypeNames = GetPluginsList();
                Type[] connectedPlugins = GetConnectedPlugins();

                VisualElement pluginsContainer = new VisualElement();
                pluginsContainer.style.marginBottom = new StyleLength(6f);
                panelContent.Add(pluginsContainer);

                pluginsContainer.Clear();

                List<string> allPluginConfigs = pluginTypeNames
                    .Where(item => !connectedPlugins.Contains(item.Value))
                    .Select(item => item.Key)
                    .ToList();
                
                if (pluginTypeNames.Length > 0) {
                    foreach(var plugin in pluginTypeNames) {
                        pluginsContainer.Add(new PluginItem(plugin.Value, connectedPlugins.Contains(plugin.Value), target));
                    }
                } else {
                    Box noPluginsContainer = new Box();
                    pluginsContainer.Add(noPluginsContainer);
                    noPluginsContainer.style.paddingTop = new StyleLength(10f);
                    noPluginsContainer.style.paddingLeft = new StyleLength(10f);
                    noPluginsContainer.style.paddingRight = new StyleLength(10f);
                    noPluginsContainer.style.paddingBottom = new StyleLength(10f);
                    Label pluginLabel = new Label("No plugins aviable");
                    noPluginsContainer.Add(pluginLabel);
                }
            } else {
                panelContent.Clear();
            }
        }

        public MonoBehaviourPluginsEditorComponent(ComposableEditor composableEditor) : base(composableEditor)
        {
        }
    }
}