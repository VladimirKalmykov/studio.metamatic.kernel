using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Metamatic;
using Metamatic.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Widget = Metamatic.Framework.Widget;

namespace Metamatic.Framework
{
    public class CursorToolsWidget: Widget
    {
        private VisualTreeAsset m_CursorToolItemTemplate;
        private Panel m_CursorToolsPanel;
        private VisualElement m_Element;
        private Dictionary<string, VisualElement> m_CursorToolItems = new ();
        public CursorToolsWidget(Overlay overlay) : base(overlay)
        {
            
        }

        public override async UniTaskVoid OnInitializeWidget(CancellationSignal signal)
        {
            m_CursorToolItemTemplate = Resources.Load<VisualTreeAsset>("Tool");
            m_CursorToolsPanel = overlay.CreatePanel("panels/cursor_tools", "Tools", new Panel.Constraints()
            {
                DefaultPreferences = new Panel.UserPreferences()
                {
                    Position = new Vector2(0, 0),
                    Size = new Vector2(120, 0)  
                },
                MaxSize = new Vector2(120, 500),
                MinSize = new Vector2(120, 60),
                AutoHeight = true,
                AutoWidth = false
            });
            overlay.root.Add(m_CursorToolsPanel);
            var contentContainer = new VisualElement();
            contentContainer.AddToClassList("VerticalItemsContainer");
            m_CursorToolsPanel.AddContent(contentContainer);
            m_Element = contentContainer;
            
            /* Subscribe to cursor tools */
            using var cursorToolSubscription = overlay.ObserveCursorTools(OnObserverCursorTool);
            using var activeCursorToolSubscription = overlay.SubscribeToActiveCursorTool(OnActiveCursorTool);
            
            
            await signal;
            Console.Debug($"CursorToolsWidget", "Release");
        }
        
        private void OnActiveCursorTool(string name)
        {
            foreach (var cursorToolItem in m_CursorToolItems)
            {
                cursorToolItem.Value.RemoveFromClassList("Active");
            }
            if (name == null) return;
            if (m_CursorToolItems.ContainsKey(name))
            {
                m_CursorToolItems[name].AddToClassList("Active");
            }
        }
        
        private async UniTaskVoid OnObserverCursorTool(CursorTool cursorTool, CancellationSignal cancellationSignal)
        {
            var name = cursorTool.Name;
            try
            {
                // Here we can add cursor tool button to the panel
                m_CursorToolItemTemplate.CloneTree(m_Element);
                var cursorToolItem = m_CursorToolsPanel.Query<VisualElement>("ToolItem").Last();
                m_CursorToolItems.Add(name, cursorToolItem);
                var icon = cursorToolItem.Query<VisualElement>("Icon").First();
                var iconImage = new Image();
                iconImage.image = Resources.Load<Texture2D>(cursorTool.IconPath);
                iconImage.AddToClassList(".IconImage");
                icon.Add(iconImage);

                var label = cursorToolItem.Query<Label>("Label").First();
                label.text = cursorTool.DisplayName;
                
                /* Listen for click at the cursor tool button */
                cursorToolItem.RegisterCallback<ClickEvent>(evt =>
                {
                    overlay.ToggleActivate(cursorTool);
                });
                
            } catch (Exception e)
            {
                Console.Error("CursorToolsWidget", e);
            }

            await cancellationSignal;
            m_CursorToolItems.Remove(name);
        }
    }
}