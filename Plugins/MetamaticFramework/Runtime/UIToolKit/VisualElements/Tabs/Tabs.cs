using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Metamatic.UIElements;
using UnityEngine.UIElements;

namespace Metamatic.UIToolKit
{
    public class Tabs<T>: AsyncVisualElement
    {
        private VisualElement m_Header;
        private VisualElement m_Content;
        private readonly Dictionary<T, VisualElement> m_TabsMap = new Dictionary<T, VisualElement>();
        private bool m_AutoSelectFirstTab = true;
        
        public override VisualElement contentContainer => m_Content;
        public readonly Observable<T> ActiveTab = new Observable<T>();

        public Tabs()
        {
            AddToClassList("MetamaticUIKit_Tabs");
            m_Header = new VisualElement();
            m_Header.AddToClassList("MetamaticUIKit_TabsHeader");
            
            m_Content = new VisualElement();
            m_Content.AddToClassList("MetamaticUIKit_TabsContent");
            
            hierarchy.Add(m_Header);
            hierarchy.Add(m_Content);
        }
        
        public DisposableAction AddTab(string title, T value)
        {
            if (m_TabsMap.ContainsKey(value))
            {
                Console.Error("Tabs", "Tab with value {0} already exists");
                return DisposableAction.Empty;
            }
            
            var tab = CreateTab(title, value);
            m_TabsMap[value] = tab;
            m_Header.Add(tab);
            if (m_AutoSelectFirstTab && m_TabsMap.Count == 1)
            {
                ActiveTab.Value = value;
            }
            return new DisposableAction(() =>
            {
                if (ActiveTab.Value != null && ActiveTab.Value.Equals(value))
                {
                    ActiveTab.Value = default;
                }
                m_TabsMap.Remove(value);
                tab.RemoveFromHierarchy();
            });
        }

        private VisualElement CreateTab(string title, T value)
        {
            var tab = new VisualElement();
            tab.AddToClassList("MetamaticUIKit_Tab");
            
            var tabTitle = new Label(title);
            tabTitle.AddToClassList("MetamaticUIKit_TabTitle");
            
            tab.Add(tabTitle);
            
            tab.RegisterCallback<ClickEvent>(e =>
            {
                ActiveTab.Value = value;
            });
            
            return tab;
        }

        public override async UniTask OnAttachedAsync(Signal detachSignal)
        {
            using var watchActiveTab = ActiveTab.Watch(OnActiveTabChanged);
            await detachSignal;
        }
        
        private void OnActiveTabChanged(T activeTab)
        {
            foreach (var tab in m_TabsMap)
            {
                if (tab.Key.Equals(activeTab))
                {
                    tab.Value.AddToClassList("MetamaticUIKit_Selected");
                }
                else
                {
                    tab.Value.RemoveFromClassList("MetamaticUIKit_Selected");
                }
            }
        }
    }
}