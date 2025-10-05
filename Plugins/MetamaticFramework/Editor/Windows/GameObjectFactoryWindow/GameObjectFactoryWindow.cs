using System;
using Cysharp.Threading.Tasks;
using Metamatic.EditorTools;
using Metamatic.Framework.Workshop;
using Metamatic.SocketSystem;
using Metamatic.UIToolKit;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic.Framework
{
    public class GameObjectFactoryWindow: AsyncEditorWindow
    {
        [UnityEditor.MenuItem("Metamatic/Framework/GameObject Factory")]
        public static void Open()
        {
            GetWindow<GameObjectFactoryWindow>("GameObject Factory");
        }
        
        protected override async UniTaskVoid EnableAsync(Func<UniTask> ready, Signal signal)
        {
            await ready();
        }

        protected override void OnCreateGUI()
        {
            rootVisualElement.Add(CreateView());
        }
        
        #region View
        private VisualElement CreateView()
        {
            var sections = new VisualElement();
            sections.Add(CreateBlueprintPoolsView());
            sections.Add(CreatePrefabPoolsView());
            sections.styleSheets.Add(UIToolKitCore.tailwindStyleSheet);
            sections.AddToClassList(UIToolKitCore.Tailwind.Padding12);
            
            return sections;
        }

        private VisualElement CreateBlueprintPoolsView()
        {
            var section = new VisualElement();
            section.AddToClassList(UIToolKitCore.Tailwind.MarginBottom24);
            var title = new Label("Blueprint Pools");
            title.AddToClassList(UIToolKitCore.Tailwind.TextSizeMd);
            title.AddToClassList(UIToolKitCore.Tailwind.MarginBottom12);
            section.Add(title);
            
            var tableView = new TableView<(ScriptableBlueprint, ConfigurablePool<GameObject>)>(
                new TableView<(ScriptableBlueprint, ConfigurablePool<GameObject>)>.Col[]
                {
                    new("Blueprint", TableView<(ScriptableBlueprint blueprint, ConfigurablePool<GameObject> pool)>.Col.WidthType.Fixed, 200, value => new ObjectField() 
                    {
                        objectType = typeof(ScriptableBlueprint),
                        value = value.Item1,
                        allowSceneObjects = false,
                        label = string.Empty
                    }),
                    new("Total", TableView<(ScriptableBlueprint, ConfigurablePool<GameObject>)>.Col.WidthType.Relative, 1, value => new Tracker<Label, ConfigurablePool<GameObject>>(new Label("-"), value.Item2, SetPoolTotalCountToLabel, PlayerLoopTiming.LastUpdate)),
                    // In pool
                    new("In Pool", TableView<(ScriptableBlueprint, ConfigurablePool<GameObject>)>.Col.WidthType.Relative, 1, value => new Tracker<Label, ConfigurablePool<GameObject>>(new Label("-"), value.Item2, SetInPoolCountToLabel, PlayerLoopTiming.LastUpdate)),
                    // Out of pool
                    new("Out of Pool", TableView<(ScriptableBlueprint, ConfigurablePool<GameObject>)>.Col.WidthType.Relative, 1, value => new Tracker<Label, ConfigurablePool<GameObject>>(new Label("-"), value.Item2, SetOutOfPoolCountToLabel, PlayerLoopTiming.LastUpdate)),
                }
            );
            tableView.Bind(GameObjectFactory.DebugBlueprintPools);
            tableView.style.flexGrow = 1;
            section.Add(tableView);
            return section;
        }
        
        private VisualElement CreatePrefabPoolsView()
        {
            var section = new VisualElement();
            section.AddToClassList(UIToolKitCore.Tailwind.MarginBottom24);
            var title = new Label("Prefab Pools");
            title.AddToClassList(UIToolKitCore.Tailwind.TextSizeMd);
            title.AddToClassList(UIToolKitCore.Tailwind.MarginBottom12);
            section.Add(title);
            
            var tableView = new TableView<(GameObject, ConfigurablePool<GameObject>)>(
                new TableView<(GameObject, ConfigurablePool<GameObject>)>.Col[]
                {
                    new("Blueprint", TableView<(GameObject blueprint, ConfigurablePool<GameObject> pool)>.Col.WidthType.Fixed, 200, value => new ObjectField() 
                    {
                        objectType = typeof(GameObject),
                        value = value.Item1,
                        allowSceneObjects = false,
                        label = string.Empty
                    }),
                    new("Total", TableView<(GameObject, ConfigurablePool<GameObject>)>.Col.WidthType.Relative, 1, value => new Tracker<Label, ConfigurablePool<GameObject>>(new Label("-"), value.Item2, SetPoolTotalCountToLabel, PlayerLoopTiming.LastUpdate)),
                    // In pool
                    new("In Pool", TableView<(GameObject, ConfigurablePool<GameObject>)>.Col.WidthType.Relative, 1, value => new Tracker<Label, ConfigurablePool<GameObject>>(new Label("-"), value.Item2, SetInPoolCountToLabel, PlayerLoopTiming.LastUpdate)),
                    // Out of pool
                    new("Out of Pool", TableView<(GameObject, ConfigurablePool<GameObject>)>.Col.WidthType.Relative, 1, value => new Tracker<Label, ConfigurablePool<GameObject>>(new Label("-"), value.Item2, SetOutOfPoolCountToLabel, PlayerLoopTiming.LastUpdate)),
                }
            );
            tableView.Bind(GameObjectFactory.DebugPrefabsPools);
            tableView.style.flexGrow = 1;
            section.Add(tableView);
            return section;
        }
        #endregion
        
        private static void SetPoolTotalCountToLabel(Label element, ConfigurablePool<GameObject> pool)
        {
            element.text = pool.TotalCount.ToString();
        }
        
        private static void SetInPoolCountToLabel(Label element, ConfigurablePool<GameObject> pool)
        {
            element.text = pool.InPoolCount.ToString();
        }
        
        private static void SetOutOfPoolCountToLabel(Label element, ConfigurablePool<GameObject> pool)
        {
            element.text = pool.OutOfPoolCount.ToString();
        }
    }
    
    
}