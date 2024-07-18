using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic
{
    [CustomEditor(typeof(Module), true)]
    public class ModuleEditor: OdinEditor
    {
        private VisualElement m_Body;
        private VisualElement m_Settings;
        private Vector3Field m_PositionElement;
        private Vector3Field m_RotationElement;
        private FieldSet m_PlugsFieldSet;
        private Label m_Type;
        private Module m_Module;
        private Button m_CopyFromLocalButton;
        private Button m_CopyToLocalButton;
        private bool m_CopyFromLocal;
        private  IVisualElementScheduledItem m_CopyFromLocalScheduledItem;
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/studio.metamatic.kernel/Editor/SocketSystem/Editors/Module/ModuleEditor.uxml");
            visualTree.CloneTree(root);
            
            m_Type = root.Q<Label>("type");
            m_Settings = root.Q<VisualElement>("settings");
            m_Body = root.Q<VisualElement>("body");
            
            var module = target as Module;
            
            if (module == null)
            {
                return root;
            }
            m_Module = module;
            
            /* Create settings view */
            CreateSettingsView(module);
            
            /* Create sockets view */
            var infos = module.GetPlugTypes();
            VisualElement[] fields = new VisualElement[infos.Length];
            
            for (int i = 0; i < infos.Length; i++)
            {
                var info = infos[i];
                var field = new GenericObjectField(info.type)
                {
                    value = module.plugs[i]
                };
                var index = i;
                field.RegisterCallback<ChangeEvent<UnityEngine.Object>>(evt =>
                {
                    module.plugs[index] = evt.newValue as Plug;
                    EditorUtility.SetDirty(module);
                });
                fields[i] = field;
            }

            m_Type.text = MiscUtils.GetGenericName(module.GetType().BaseType, true, true);

            var fieldSet = new FieldSet("Plugs", fields);
            root.Add(fieldSet);
            
            /* Create connected module view */
            m_PlugsFieldSet = new FieldSet("Connections", null, true, false);
            root.Add(m_PlugsFieldSet);
            
            m_PlugsFieldSet.style.display = Application.isPlaying ? DisplayStyle.Flex : DisplayStyle.None;

            UpdateModulesView();
            
            return root;
        }
        
        private void CreateSettingsView(Module module)
        {
            /* Autoconnect */
            var autoConnectProp = serializedObject.FindProperty("autoConnect");
            var autoConnectToggle = new Toggle("Auto Connect")
            {
                value = autoConnectProp.boolValue
            };
            autoConnectToggle.RegisterValueChangedCallback(evt =>
            {
                autoConnectProp.boolValue = evt.newValue;
                serializedObject.ApplyModifiedProperties();
            });
            m_Settings.Add(autoConnectToggle);
            autoConnectToggle.SetEnabled(!Application.isPlaying);
            
            /* Sticky */
            var stickyProp = serializedObject.FindProperty("sticky");
            var stickyToggle = new Toggle("Sticky")
            {
                value = stickyProp.boolValue
            };
            stickyToggle.RegisterValueChangedCallback(evt =>
            {
                stickyProp.boolValue = evt.newValue;
                serializedObject.ApplyModifiedProperties();
                UpdateView();
            });
            m_Settings.Add(stickyToggle);
            stickyToggle.SetEnabled(!Application.isPlaying);
            
            /* Position */
            var positionProp = serializedObject.FindProperty("relativePosition");
            m_PositionElement = new Vector3Field("Position")
            {
                value = positionProp.vector3Value
            };
            m_PositionElement.RegisterValueChangedCallback(evt =>
            {
                positionProp.vector3Value = evt.newValue;
                serializedObject.ApplyModifiedProperties();
            });
            m_Settings.Add(m_PositionElement);
            m_PositionElement.SetEnabled(!Application.isPlaying);
            
            /* Rotation */
            var rotationProp = serializedObject.FindProperty("relativeRotation");
            m_RotationElement = new Vector3Field("Rotation")
            {
                value = rotationProp.quaternionValue.eulerAngles
            };
            m_RotationElement.RegisterValueChangedCallback(evt =>
            {
                rotationProp.quaternionValue = Quaternion.Euler(evt.newValue);
                serializedObject.ApplyModifiedProperties();
            });
            m_Settings.Add(m_RotationElement);
            m_RotationElement.SetEnabled(!Application.isPlaying);
            
            /* Copy from local position and rotation */
            m_CopyFromLocalButton = new Button(() =>
            {
                m_CopyFromLocal = !m_CopyFromLocal;
                m_PositionElement.SetEnabled(!m_CopyFromLocal);
                m_RotationElement.SetEnabled(!m_CopyFromLocal);
                if (m_CopyFromLocal)
                {
                    if (m_CopyFromLocalScheduledItem != null)
                    {
                        m_CopyFromLocalScheduledItem.Pause();
                    }
                    ApplyCopyFromLocal();
                    m_CopyFromLocalScheduledItem = m_CopyFromLocalButton.schedule.Execute(() => ApplyCopyFromLocal()).Every(100);
                }
                else
                {
                    if (m_CopyFromLocalScheduledItem != null)
                    {
                        m_CopyFromLocalScheduledItem.Pause();
                        m_CopyFromLocalScheduledItem = null;
                    }
                }
                serializedObject.ApplyModifiedProperties();
                UpdateView();
            })
            {
                text = "Copy from local"
            };
            
            /* Copy to local position and rotation */
            m_CopyToLocalButton = new Button(() =>
            {
                ApplyCopyToLocal();
            })
            {
                text = "Copy to local"
            };
            
            m_Settings.Add(m_CopyFromLocalButton);
            m_Settings.Add(m_CopyToLocalButton);
            m_CopyFromLocalButton.SetEnabled(!Application.isPlaying);
            m_CopyToLocalButton.SetEnabled(!Application.isPlaying);

            UpdateView();
        }
        
        private void ApplyCopyToLocal()
        {
            var positionProp = serializedObject.FindProperty("relativePosition");
            var rotationProp = serializedObject.FindProperty("relativeRotation");
            var nextLocalPosition = m_PositionElement.value;
            var nextLocalRotation = Quaternion.Euler(m_RotationElement.value);
            m_Module.transform.localPosition = nextLocalPosition;
            m_Module.transform.localRotation = nextLocalRotation;
            positionProp.vector3Value = nextLocalPosition;
            rotationProp.quaternionValue = nextLocalRotation;
            serializedObject.ApplyModifiedProperties();
        }

        private void ApplyCopyFromLocal()
        {
            var positionProp = serializedObject.FindProperty("relativePosition");
            var rotationProp = serializedObject.FindProperty("relativeRotation");
            var nextLocalPosition = m_Module.transform.localPosition;
            var nextLocalRotation = m_Module.transform.localRotation;
            positionProp.vector3Value = nextLocalPosition;
            rotationProp.quaternionValue = nextLocalRotation;
            m_PositionElement.value = nextLocalPosition;
            m_RotationElement.value = nextLocalRotation.eulerAngles;
            serializedObject.ApplyModifiedProperties();
        }

        private void UpdateView()
        {
            var stickyProp = serializedObject.FindProperty("sticky");
            var positionProp = serializedObject.FindProperty("relativePosition");
            var rotationProp = serializedObject.FindProperty("relativeRotation");
            m_PositionElement.style.display = stickyProp.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
            m_RotationElement.style.display = stickyProp.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
            m_CopyFromLocalButton.style.display = stickyProp.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
            m_CopyToLocalButton.style.display = stickyProp.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
            m_PositionElement.value = positionProp.vector3Value;
            m_RotationElement.value = rotationProp.quaternionValue.eulerAngles;
            if (m_CopyFromLocal)
            {
                m_CopyFromLocalButton.AddToClassList("Active");
            }
            else
            {
                m_CopyFromLocalButton.RemoveFromClassList("Active");
            }
        }
        
        protected override void OnEnable()
        {
            m_Module = target as Module;
            if (m_Module == null)
            {
                return;
            }
            m_Module.OnReconnect += UpdateModulesView;
            if (m_PlugsFieldSet != null)
            {
                m_PlugsFieldSet.style.display = Application.isPlaying ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        
        protected override void OnDisable()
        {
            m_CopyFromLocalScheduledItem?.Pause();
            m_CopyFromLocalScheduledItem = null;
            m_Module.OnReconnect -= UpdateModulesView;
            if (m_PlugsFieldSet != null)
            {
                m_PlugsFieldSet.style.display = DisplayStyle.None;
            }
        }

        private void UpdateModulesView()
        {
            if (m_PlugsFieldSet == null)
            {
                return;
            }
            var connectedSlot = m_Module.connectedSlot;
            m_PlugsFieldSet.isActive = connectedSlot != null;
            if (connectedSlot == null)
            {
                m_PlugsFieldSet.content = new[]
                {
                    new Label("No slots connected") as VisualElement
                };
            }
            else
            {
                m_PlugsFieldSet.content = new[]
                {
                    new GenericObjectField(connectedSlot.GetType(), true)
                    {
                        value = connectedSlot
                    }
                };
            }
        }
    }
}