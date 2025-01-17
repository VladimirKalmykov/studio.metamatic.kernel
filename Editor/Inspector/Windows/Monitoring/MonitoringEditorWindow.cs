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
            GetWindow(typeof(MonitoringEditorWindow), false, "Metamatic Monitoring");
        }
        
        private VisualTreeAsset m_RowTemplate;
        private VisualTreeAsset m_PanelTemplate;
        private VisualTreeAsset m_KeyValueTemplate;

        private VisualElement m_RowElement;
        private event Action OnUpdate;

        protected override void OnCreateGUI()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/studio.metamatic.kernel/Editor/Inspector/Windows/Monitoring/Monitoring.uxml");
            m_RowTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/studio.metamatic.kernel/Editor/Inspector/Windows/Monitoring/Row.uxml");
            m_PanelTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/studio.metamatic.kernel/Editor/Inspector/Windows/Monitoring/Panel.uxml");
            m_KeyValueTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/studio.metamatic.kernel/Editor/Inspector/Windows/Monitoring/KeyValue.uxml");
            
            asset.CloneTree(rootVisualElement);
            
            /* Add row */
            var content = rootVisualElement.Q<ScrollView>("Content");
            m_RowElement = content.Query<VisualElement>("Row").Last();

        }

        protected override async UniTaskVoid EnableAsync(Func<UniTask> ready, Signal signal)
        {
            /* Listen for monitoring data */
            using var observeGroups = Monitoring.ObserveGroups(OnGroupAdded);
            
            await ready();
        }
        
        private void Update()
        {
            OnUpdate?.Invoke();
        }

        private async UniTaskVoid OnGroupAdded(Monitoring.Group group, Signal signal)
        {
            /* Add panel */
            m_PanelTemplate.CloneTree(m_RowElement);
            var panel = m_RowElement.Query<VisualElement>("Panel").Last();
            panel.Q<Label>("Title").text = group.Name;
            /* Observe items */
            using var observeItems = group.ObserveItems((item, cancellationSignal) => OnItemAdded(panel, item, cancellationSignal));
            await signal;
            panel.RemoveFromHierarchy();
        }
        
        private async UniTaskVoid OnItemAdded(VisualElement panel, Monitoring.MonitoringItem item, Signal signal)
        {
            /* Add key value */
            var panelContent = panel.Q<VisualElement>("Content");
            m_KeyValueTemplate.CloneTree(panelContent);
            var keyValue = panelContent.Query<VisualElement>("KeyValue").Last();
            keyValue.Q<Label>("Key").text = item.Key;
            var value = keyValue.Q<VisualElement>("Value");
            var drawer = DrawerVisualElement.Create(item.Type, false, signal);
            await drawer.ReadyTask;
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