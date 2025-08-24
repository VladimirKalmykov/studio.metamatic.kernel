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
        private IVisualElementScheduledItem m_CopyFromLocalScheduledItem;
        private bool m_CopyFromLocal;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Packages/studio.metamatic.kernel/Editor/SocketSystem/Editors/Module/ModuleEditor.uxml");
            visualTree.CloneTree(root);

            m_Type = root.Q<Label>("type");
            m_Settings = root.Q<VisualElement>("settings");
            m_Body = root.Q<VisualElement>("body");

            m_Module = target as Module;
            if (m_Module == null) return root;

            CreateSettingsView(m_Module);
            m_Type.text = MiscUtils.GetGenericName(m_Module.GetType().BaseType, true, true);

            m_PlugsFieldSet = new FieldSet("Connections", null, true, false);
            root.Add(m_PlugsFieldSet);
            m_PlugsFieldSet.style.display = Application.isPlaying ? DisplayStyle.Flex : DisplayStyle.None;

            UpdateModulesView(m_Module.connectedSlot);
            return root;
        }

        private void CreateSettingsView(Module module)
        {
            // autoConnect
            var autoConnectProp = serializedObject.FindProperty("autoConnect");
            var autoConnectToggle = new Toggle("Auto Connect") { value = autoConnectProp.boolValue };
            autoConnectToggle.RegisterValueChangedCallback(evt => { autoConnectProp.boolValue = evt.newValue; serializedObject.ApplyModifiedProperties(); });
            m_Settings.Add(autoConnectToggle);
            autoConnectToggle.SetEnabled(!Application.isPlaying);

            // sticky
            var stickyProp = serializedObject.FindProperty("sticky");
            var stickyToggle = new Toggle("Sticky") { value = stickyProp.boolValue };
            stickyToggle.RegisterValueChangedCallback(evt => { stickyProp.boolValue = evt.newValue; serializedObject.ApplyModifiedProperties(); UpdateView(); });
            m_Settings.Add(stickyToggle);
            stickyToggle.SetEnabled(!Application.isPlaying);

            // relativePosition
            var positionProp = serializedObject.FindProperty("relativePosition");
            m_PositionElement = new Vector3Field("Position") { value = positionProp.vector3Value };
            m_PositionElement.RegisterValueChangedCallback(evt => { positionProp.vector3Value = evt.newValue; serializedObject.ApplyModifiedProperties(); });
            m_Settings.Add(m_PositionElement);
            m_PositionElement.SetEnabled(!Application.isPlaying);

            // relativeRotation
            var rotationProp = serializedObject.FindProperty("relativeRotation");
            m_RotationElement = new Vector3Field("Rotation") { value = rotationProp.quaternionValue.eulerAngles };
            m_RotationElement.RegisterValueChangedCallback(evt => { rotationProp.quaternionValue = Quaternion.Euler(evt.newValue); serializedObject.ApplyModifiedProperties(); });
            m_Settings.Add(m_RotationElement);
            m_RotationElement.SetEnabled(!Application.isPlaying);

            // Copy from local (polling)
            m_CopyFromLocalButton = new Button(() =>
            {
                m_CopyFromLocal = !m_CopyFromLocal;
                m_PositionElement.SetEnabled(!m_CopyFromLocal);
                m_RotationElement.SetEnabled(!m_CopyFromLocal);

                if (m_CopyFromLocal)
                {
                    m_CopyFromLocalScheduledItem?.Pause();
                    ApplyCopyFromLocal();
                    m_CopyFromLocalScheduledItem = m_CopyFromLocalButton.schedule.Execute(ApplyCopyFromLocal).Every(100);
                }
                else
                {
                    m_CopyFromLocalScheduledItem?.Pause();
                    m_CopyFromLocalScheduledItem = null;
                }

                serializedObject.ApplyModifiedProperties();
                UpdateView();
            })
            { text = "Copy from local" };

            // Copy to local (one-shot)
            m_CopyToLocalButton = new Button(ApplyCopyToLocal) { text = "Copy to local" };

            // Bake from parent slot (новая корректная математика)
            m_SetRelativeParentSlotButton = new Button(SetFromParentSlot)
            { text = "Bake position and rotation from parent slot" };

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

        /// <summary>Правильный bake: считаем относительные pos/rot в базисе виртуального якоря.</summary>
        private void SetFromParentSlot()
        {
            var module = (Module)target;
            if (module == null) return;

            if (module.TryFindAppropriateSlotInParent(out var slot))
            {
                // мир модуля
                var mPos = module.transform.position;
                var mRot = module.transform.rotation;

                // мир виртуального якоря: T_slot * (pos/rot якоря в локале слота)
                var slotAnchorPos = slot.transform.TransformPoint(slot.anchorPosition);
                var slotAnchorRot = slot.transform.rotation * GetReflectedRotation(slot.anchorRotation, slot.transform);

                // относительная позиция в базисе якоря
                var relPosWorld = mPos - slotAnchorPos;
                var relPos = Quaternion.Inverse(slotAnchorRot) * relPosWorld;

                // относительный поворот
                var relRot = Quaternion.Inverse(slotAnchorRot) * mRot;

                // записываем
                var p = serializedObject.FindProperty("relativePosition");
                var r = serializedObject.FindProperty("relativeRotation");
                p.vector3Value = relPos;
                r.quaternionValue = relRot;
                serializedObject.ApplyModifiedProperties();

                // обновляем UI
                m_PositionElement.value = relPos;
                m_RotationElement.value = relRot.eulerAngles;

                Debug.Log($"[Bake] relativePosition={relPos}, relativeRotation(euler)={relRot.eulerAngles}");
            }
            else
            {
                Debug.LogWarning("No appropriate slot found in parent");
            }
        }

        /// <summary>Получить отраженный поворот с учетом отражения объекта в мировом пространстве</summary>
        private Quaternion GetReflectedRotation(Quaternion localRotation, Transform transform)
        {
            // Получаем матрицу трансформации в мировом пространстве
            Matrix4x4 worldMatrix = transform.localToWorldMatrix;

            // Определяем отражение по знаку определителя матрицы
            float determinant = worldMatrix.determinant;
            bool isReflected = determinant < 0;

            if (isReflected)
            {
                // При отражении инвертируем все компоненты кватерниона
                return new Quaternion(
                    -localRotation.x,
                    -localRotation.y,
                    -localRotation.z,
                    localRotation.w
                );
            }

            return localRotation;
        }

        private void UpdateView()
        {
            var stickyProp = serializedObject.FindProperty("sticky");
            var positionProp = serializedObject.FindProperty("relativePosition");
            var rotationProp = serializedObject.FindProperty("relativeRotation");

            var on = stickyProp.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
            m_PositionElement.style.display = on;
            m_RotationElement.style.display = on;
            m_CopyFromLocalButton.style.display = on;
            m_CopyToLocalButton.style.display = on;
            m_SetRelativeParentSlotButton.style.display = on;

            m_PositionElement.value = positionProp.vector3Value;
            m_RotationElement.value = rotationProp.quaternionValue.eulerAngles;

            if (m_CopyFromLocal) m_CopyFromLocalButton.AddToClassList("Active");
            else m_CopyFromLocalButton.RemoveFromClassList("Active");
        }

        protected override void OnEnable()
        {
            m_Module = target as Module;
            if (m_Module == null) return;

            m_Module.OnReconnect += UpdateModulesView;
            if (m_PlugsFieldSet != null)
                m_PlugsFieldSet.style.display = Application.isPlaying ? DisplayStyle.Flex : DisplayStyle.None;
        }

        protected override void OnDisable()
        {
            m_CopyFromLocalScheduledItem?.Pause();
            m_CopyFromLocalScheduledItem = null;

            if (m_Module != null) m_Module.OnReconnect -= UpdateModulesView;
            if (m_PlugsFieldSet != null) m_PlugsFieldSet.style.display = DisplayStyle.None;
        }

        private void UpdateModulesView(Slot connectedSlot)
        {
            if (m_PlugsFieldSet == null) return;

            m_PlugsFieldSet.isActive = connectedSlot != null;
            m_PlugsFieldSet.content = connectedSlot == null
                ? new VisualElement[] { new Label("No slots connected") }
                : new VisualElement[] { new GenericObjectField(connectedSlot.GetType(), true) { value = connectedSlot } };
        }
    }
}
