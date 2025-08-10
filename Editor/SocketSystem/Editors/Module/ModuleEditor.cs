using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic.SocketSystem
{
    [CustomEditor(typeof(Module), true)]
    public class ModuleEditor : OdinEditor
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
        private Button m_SetRelativeParentSlotButton;
        private bool m_CopyFromLocal;
        private IVisualElementScheduledItem m_CopyFromLocalScheduledItem;
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

            m_Type.text = MiscUtils.GetGenericName(module.GetType().BaseType, true, true);

            /* Create connected module view */
            m_PlugsFieldSet = new FieldSet("Connections", null, true, false);
            root.Add(m_PlugsFieldSet);

            m_PlugsFieldSet.style.display = Application.isPlaying ? DisplayStyle.Flex : DisplayStyle.None;

            UpdateModulesView(m_Module.connectedSlot);

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

            /* Set from relative parent slot */
            m_SetRelativeParentSlotButton = new Button(() =>
            {
                SetFromParentSlot();
            })
            {
                text = "Bake position and rotation from parent slot"
            };

            m_Settings.Add(m_CopyFromLocalButton);
            m_Settings.Add(m_CopyToLocalButton);
            m_Settings.Add(m_SetRelativeParentSlotButton);
            m_CopyFromLocalButton.SetEnabled(!Application.isPlaying);
            m_CopyToLocalButton.SetEnabled(!Application.isPlaying);
            m_SetRelativeParentSlotButton.SetEnabled(!Application.isPlaying);

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

        private void SetFromParentSlot()
        {
            /* First, search Slot<TFormFactor> in the parent */
            var module = target as Module;
            if (module == null)
            {
                return;
            }
            if (module.TryFindAppropriateSlotInParent(out var slot))
            {
                /* Get current world position and rotation of the module */
                var moduleWorldPosition = module.transform.position;
                var moduleWorldRotation = module.transform.rotation;

                /* Get slot's world position and rotation */
                var slotWorldPosition = slot.transform.TransformPoint(slot.anchorPosition);
                var slotWorldRotation = slot.transform.rotation * slot.anchorRotation;

                /* Calculate relative position */
                var relativeWorldPosition = moduleWorldPosition - slotWorldPosition;
                var relativePosition = Quaternion.Inverse(slotWorldRotation) * relativeWorldPosition;

                /* Calculate relative rotation */
                /* Since SumQuaternions adds Euler angles, we need to subtract slot's rotation from module's rotation */
                var slotEuler = slotWorldRotation.eulerAngles;
                var moduleEuler = moduleWorldRotation.eulerAngles;
                var relativeEuler = moduleEuler - slotEuler;

                /* Normalize angles to 0-360 range */
                relativeEuler = new Vector3(
                    (relativeEuler.x + 360) % 360,
                    (relativeEuler.y + 360) % 360,
                    (relativeEuler.z + 360) % 360
                );
                var relativeRotation = Quaternion.Euler(relativeEuler);

                /* Update module's serialized properties */
                var positionProp = serializedObject.FindProperty("relativePosition");
                var rotationProp = serializedObject.FindProperty("relativeRotation");

                positionProp.vector3Value = relativePosition;
                rotationProp.quaternionValue = relativeRotation;

                /* Update UI elements */
                m_PositionElement.value = relativePosition;
                m_RotationElement.value = relativeRotation.eulerAngles;

                serializedObject.ApplyModifiedProperties();

                Debug.Log($"SetFromParentSlot: Calculated relativePosition={relativePosition}, relativeRotation={relativeRotation.eulerAngles}");
                Debug.Log($"Slot anchor: {slot.anchorPosition}, Slot rotation: {slot.anchorRotation.eulerAngles}");
                Debug.Log($"Module world: {moduleWorldPosition}, Module rotation: {moduleWorldRotation.eulerAngles}");
            }
            else
            {
                Debug.LogWarning("No appropriate slot found in parent");
            }
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
            m_SetRelativeParentSlotButton.style.display = stickyProp.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
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

        private void UpdateModulesView(Slot connectedSlot)
        {
            if (m_PlugsFieldSet == null)
            {
                return;
            }
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