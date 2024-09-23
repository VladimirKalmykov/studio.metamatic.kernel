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
        private readonly Observable<Type> m_ActiveSpellType = new ();
        private readonly Dictionary<Type, VisualElement> m_MapSpellTypeToElement = new ();
        private Panel m_SpellPanel;
        public SpellsWidget(MetamaticFrameworkOverlay overlay) : base(overlay)
        {
        }

        public override async UniTaskVoid OnInitializeWidget(CancellationSignal signal)
        {
            m_ItemTemplate = Resources.Load<VisualTreeAsset>("SelectableItem");
            /* Create spell panel */
            m_SpellPanel = overlay.CreatePanel("panels/spells_widget/spell", "Spell", new Panel.Constraints()
            {
                DefaultPreferences = new Panel.UserPreferences()
                {
                    Position = new Vector2(200, 200),
                    Size = new Vector2(300, 0)  
                },
                MaxSize = new Vector2(500, 600),
                MinSize = new Vector2(300, 120),
                AutoHeight = true,
                AutoWidth = false
            });
            overlay.root.Add(m_SpellPanel);
            
            
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
                
                /* Capture click at indicator */
                void ClickHandler(MouseUpEvent evt)
                {
                    m_ActiveSpellType.value = spell.SpellType;
                }
                item.RegisterCallback<MouseUpEvent>(ClickHandler);
            }
            
            /* Subscribe to active spell type */
            using var activeSpellTypeSubscription = m_ActiveSpellType.Subscribe(OnActiveSpellTypeChanged);
            
            await signal;
            overlay.root.Remove(panel);
            overlay.root.Remove(m_SpellPanel);
        }
        
        private void OnActiveSpellTypeChanged(Type spellType)
        {
            foreach (var element in m_MapSpellTypeToElement.Values)
            {
                element.RemoveFromClassList("Selected");
            }
            
            if (m_MapSpellTypeToElement.TryGetValue(spellType, out var selectedElement))
            {
                selectedElement.AddToClassList("Selected");
                m_SpellPanel.title = spellType.Name;
            }
            else
            {
                m_SpellPanel.title = "Spell";
            }
        }
    }
}