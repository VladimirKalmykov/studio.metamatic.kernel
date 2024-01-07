using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic
{
    [CustomEditor(typeof(Slot), true)]
    public class SlotEditor: OdinEditor
    {
        private VisualElement m_Body;
        private VisualElement m_Settings;
        private FieldSet m_ModulesFieldSet;
        private Label m_Type;
        private Slot m_Slot;
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/studio.metamatic.kernel/Editor/SocketSystem/Editors/Slot/SlotEditor.uxml");
            visualTree.CloneTree(root);
            
            m_Type = root.Q<Label>("type");
            m_Settings = root.Q<VisualElement>("settings");
            m_Body = root.Q<VisualElement>("body");
            
            
            
            var slot = target as Slot;
            
            if (slot == null)
            {
                return root;
            }
            m_Slot = slot;
            
            /* Create settings view */
            CreateSettingsView(slot);
            
            /* Create sockets view */
            var infos = slot.GetSocketSchemesTypes();
            VisualElement[] fields = new VisualElement[infos.Length];
            
            for (int i = 0; i < infos.Length; i++)
            {
                var info = infos[i];
                var field = new GenericObjectField(info.type)
                {
                    value = slot.sockets[i]
                };
                var index = i;
                field.RegisterCallback<ChangeEvent<UnityEngine.Object>>(evt =>
                {
                    slot.sockets[index] = evt.newValue as Socket;
                    EditorUtility.SetDirty(slot);
                });
                fields[i] = field;
            }

            m_Type.text = MiscUtils.GetGenericName(slot.GetType().BaseType, true, true);

            var fieldSet = new FieldSet("Sockets", fields);
            root.Add(fieldSet);
            
            /* Create connected module view */
            m_ModulesFieldSet = new FieldSet("Connections", null, true, false);
            root.Add(m_ModulesFieldSet);
            
            m_ModulesFieldSet.style.display = Application.isPlaying ? DisplayStyle.Flex : DisplayStyle.None;

            UpdateModulesView();
            
            return root;
        }

        private void CreateSettingsView(Slot slot)
        {
            /* Allow multi connections */
            var allowMultipleConnectionsProperty = serializedObject.FindProperty("allowMultipleConnections");
            var allowMultiConnectionsToggle = new Toggle("Allow multi connections")
            {
                value = allowMultipleConnectionsProperty.boolValue
            };
            allowMultiConnectionsToggle.RegisterValueChangedCallback(evt =>
            {
                allowMultipleConnectionsProperty.boolValue = evt.newValue;
                serializedObject.ApplyModifiedProperties();
            });
            m_Settings.Add(allowMultiConnectionsToggle);
            allowMultiConnectionsToggle.SetEnabled(m_Slot.isSupportMultiConnections && !Application.isPlaying);
            
            /* Dedicated */
            var dedicatedProperty = serializedObject.FindProperty("dedicated");
            var dedicatedToggle = new Toggle("Dedicated")
            {
                value = dedicatedProperty.boolValue
            };
            dedicatedToggle.RegisterValueChangedCallback(evt =>
            {
                dedicatedProperty.boolValue = evt.newValue;
                serializedObject.ApplyModifiedProperties();
            });
            m_Settings.Add(dedicatedToggle);
            
            /* Sticky position */
            var stickyPositionProperty = serializedObject.FindProperty("m_AnchorPosition");
            var stickyPositionToggle = new Vector3Field("Position")
            {
                value = stickyPositionProperty.vector3Value
            };
            stickyPositionToggle.RegisterValueChangedCallback(evt =>
            {
                stickyPositionProperty.vector3Value = evt.newValue;
                serializedObject.ApplyModifiedProperties();
            });
            m_Settings.Add(stickyPositionToggle);
            stickyPositionToggle.SetEnabled(!Application.isPlaying);
            
            /* Sticky rotation */
            var stickyRotationProperty = serializedObject.FindProperty("m_AnchorRotation");
            var stickyRotationToggle = new Vector3Field("Rotation")
            {
                value = stickyRotationProperty.quaternionValue.eulerAngles
            };
            stickyRotationToggle.RegisterValueChangedCallback(evt =>
            {
                stickyRotationProperty.quaternionValue = Quaternion.Euler(evt.newValue);
                serializedObject.ApplyModifiedProperties();
            });
            m_Settings.Add(stickyRotationToggle);
            stickyRotationToggle.SetEnabled(!Application.isPlaying);
            UpdateView();
        }
        
        private void UpdateView()
        {
            
        }
        
        protected override void OnEnable()
        {
            m_Slot = target as Slot;
            if (m_Slot == null)
            {
                return;
            }
            m_Slot.OnReconnect += UpdateModulesView;
            if (m_ModulesFieldSet != null)
            {
                m_ModulesFieldSet.style.display = Application.isPlaying ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        
        protected override void OnDisable()
        {
            m_Slot.OnReconnect -= UpdateModulesView;
            if (m_ModulesFieldSet != null)
            {
                m_ModulesFieldSet.style.display = DisplayStyle.None;
            }
        }

        private void UpdateModulesView()
        {
            if (m_ModulesFieldSet == null) return;
            var connectedModule = m_Slot.connectedModules;
            m_ModulesFieldSet.isActive = connectedModule.Any();
            if (!connectedModule.Any())
            {
                m_ModulesFieldSet.content = new[]
                {
                    new Label("No modules connected") as VisualElement
                };
            }
            else
            {
                m_ModulesFieldSet.content = connectedModule.Select(module => new GenericObjectField(module.GetType(), true)
                {
                    value = module
                }).Cast<VisualElement>().ToArray();
            }
        }
    }
}