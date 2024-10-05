using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic.Framework.Widgets
{
    [MetamaticWidget("Metamatic/Tools", null, WidgetApplicability.Viewport)]
    public class ToolsWidget: Widget
    {
        private Overlay m_Overlay;
        private VisualTreeAsset m_CursorToolItemTemplate;
        private VisualElement m_Element;
        private Dictionary<string, VisualElement> m_CursorToolItems = new ();
        
        public ToolsWidget()
        {
            m_CursorToolItemTemplate = Resources.Load<VisualTreeAsset>("Tool");
            
            var content = new VisualElement();
            content.AddToClassList("VerticalItemsContainer");
            Add(content);
            m_Element = content;
        }
        public override async UniTask OnPanelAttachedAsync(CancellationSignal cancellationSignal)
        {
            var overlay = m_Overlay = this.GetContext<Overlay>().Value;
            /* Subscribe to cursor tools */
            using var cursorToolSubscription = overlay.ObserveCursorTools(OnObserverCursorTool);
            using var activeCursorToolSubscription = overlay.SubscribeToActiveCursorTool(OnActiveCursorTool);
            
            
            await cancellationSignal;
        }

        public override Preferences GetDefaultPreferences()
        {
            return new Preferences(new UnityEngine.Vector2(0, 120), new UnityEngine.Vector2(120, 0), new UnityEngine.Vector2(120, 0), new UnityEngine.Vector2(120, 0));
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
                var cursorToolItem = this.Query<VisualElement>("ToolItem").Last();
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
                    m_Overlay.ToggleActivate(cursorTool);
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