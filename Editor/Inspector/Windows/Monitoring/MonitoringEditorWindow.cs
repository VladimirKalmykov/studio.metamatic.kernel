using System;
using Cysharp.Threading.Tasks;
using Metamatic.DevTools;
using Metamatic.EditorTools;
using UnityEditor;
using UnityEngine.UIElements;

namespace Metamatic.Inspector
{
    public class MonitoringEditorWindow: AsyncEditorWindow
    {
        [MenuItem ("Window/Metamatic/Monitoring")]
        public static void ShowWindow () {
            GetWindow(typeof(MonitoringEditorWindow), false, "Metamatic Console");
        }
        
        private VisualTreeAsset m_RowTemplate;
        private VisualTreeAsset m_PanelTemplate;
        private VisualTreeAsset m_KeyValueTemplate;

        private VisualElement m_RowElement;
        private event Action OnUpdate;
        
        protected override async UniTaskVoid InitializeAsync(Func<CancellationTokenAwaitable> ready, CancellationSignal signal)
        {
            await ready();
        }

        protected override async UniTaskVoid CreateGUIAsync(Func<CancellationTokenAwaitable> ready, CancellationSignal signal)
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/studio.metamatic.kernel/Editor/Inspector/Windows/Monitoring/Monitoring.uxml");
            m_RowTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/studio.metamatic.kernel/Editor/Inspector/Windows/Monitoring/Row.uxml");
            m_PanelTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/studio.metamatic.kernel/Editor/Inspector/Windows/Monitoring/Panel.uxml");
            m_KeyValueTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/studio.metamatic.kernel/Editor/Inspector/Windows/Monitoring/KeyValue.uxml");
            
            asset.CloneTree(rootVisualElement);
            
            /* Add row */
            var content = rootVisualElement.Q<ScrollView>("Content");
            var row = m_RowElement = m_RowTemplate.CloneTree();
            content.Add(row);
            
            /* Listen for monitoring data */
            using var observeGroups = Monitoring.ObserveGroups(OnGroupAdded);
            
            await ready();
        }
        
        private void Update()
        {
            OnUpdate?.Invoke();
        }

        private async UniTaskVoid OnGroupAdded(Monitoring.Group group, CancellationSignal signal)
        {
            /* Add panel */
            var panel = m_PanelTemplate.CloneTree();
            m_RowElement.Add(panel);
            panel.Q<Label>("Title").text = group.Name;
            /* Observe items */
            using var observeItems = group.ObserveItems((item, cancellationSignal) => OnItemAdded(panel, item, cancellationSignal));
            await signal;
            panel.RemoveFromHierarchy();
        }
        
        private async UniTaskVoid OnItemAdded(VisualElement panel, Monitoring.MonitoringItem item, CancellationSignal signal)
        {
            /* Add key value */
            var keyValue = m_KeyValueTemplate.CloneTree();
            panel.Q<VisualElement>("Content").Add(keyValue);
            keyValue.Q<Label>("Key").text = item.Key;
            var value = keyValue.Q<VisualElement>("Value");
            var drawer = DrawerVisualElement.Create(item.Type, false, signal);
            value.Add(drawer);
            void UpdateHandler()
            {
                drawer.SetObjectValue(item.Value);
            }
            OnUpdate += UpdateHandler;
            await signal;
            OnUpdate -= UpdateHandler;
            keyValue.RemoveFromHierarchy();
        }
    }
}