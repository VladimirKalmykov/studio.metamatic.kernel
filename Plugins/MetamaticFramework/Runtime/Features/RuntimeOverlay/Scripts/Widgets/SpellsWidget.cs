using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic.Framework
{
    public class SpellsWidget: Widget
    {
        private VisualTreeAsset m_ItemTemplate;
        private readonly Dictionary<Type, VisualElement> m_MapSpellTypeToElement = new ();
       
        
        public SpellsWidget(Overlay overlay) : base(overlay)
        {
        }

        public override async UniTaskVoid OnInitializeWidget(CancellationSignal signal)
        {
            m_ItemTemplate = Resources.Load<VisualTreeAsset>("SelectableItem");
            
            
            
            /* Create spells panel */
            var panel = overlay.CreatePanel("panels/spells_widget/spells", "Spells", new Panel.Constraints()
            {
                DefaultPreferences = new Panel.UserPreferences()
                {
                    Position = new Vector2(200, 200),
                    Size = new Vector2(200, 0)  
                },
                MaxSize = new Vector2(500, 600),
                MinSize = new Vector2(200, 40),
                AutoHeight = true,
                AutoWidth = false
            });
            overlay.root.Add(panel);
            
            var root = new VisualElement();
            root.AddToClassList("VerticalItemsContainer");
            panel.AddContent(root);

            var allSpells = overlay.core.spellMetadata;
            foreach (var spell in allSpells)
            {
                m_ItemTemplate.CloneTree(root);
                var item = root.Query<VisualElement>("Item").Last();
                m_MapSpellTypeToElement[spell.SpellType] = item;
                var led = item.Query<VisualElement>("Led").First();
                
                var title = item.Query<Label>("Title").First();
                title.text = spell.Name;
                var indicator = item.Query<VisualElement>("Indicator").First();
                
                /* Capture click */
                void ClickHandler(MouseUpEvent evt)
                {
                    overlay.core.SetActiveSpellType(spell.SpellType);
                }
                item.RegisterCallback<MouseUpEvent>(ClickHandler);
            }
            
            /* Subscribe to active spell type */
            using var activeSpellTypeSubscription = overlay.core.WatchActiveSpellType(OnActiveSpellTypeChanged);
            
            await signal;
            overlay.root.Remove(panel);
        }
        
        private void OnActiveSpellTypeChanged(Type spellType)
        {
            foreach (var element in m_MapSpellTypeToElement.Values)
            {
                element.RemoveFromClassList("Selected");
            }

            if (spellType == null) return;
            if (m_MapSpellTypeToElement.TryGetValue(spellType, out var selectedElement))
            {
                selectedElement.AddToClassList("Selected");
                
            }
        }

        
    }
}