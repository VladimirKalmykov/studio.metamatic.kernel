using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic.Framework.Widgets
{
    [MetamaticWidget("Metamatic/Spells", null, WidgetApplicability.Viewport)]
    public class SpellsWidget: Widget
    {
        private VisualTreeAsset m_ItemTemplate;
        private readonly Dictionary<Type, VisualElement> m_MapSpellTypeToElement = new ();
        private VisualElement m_SpellsContainer;
        private VisualElement m_SpellContainer;
        
        // Spell
        private Form m_SpellForm;
        private Type m_ActiveSpellType;
        private List<string> m_Messages = new List<string>();
        private EditorFormElement m_CurrentEditor;
        
        public SpellsWidget()
        {
            m_ItemTemplate = Resources.Load<VisualTreeAsset>("SelectableItem");
            var splitter = new TwoPaneSplitView(0, 200, TwoPaneSplitViewOrientation.Horizontal);

            // Create spells pane
            var spellsContainer = m_SpellsContainer = new VisualElement();
            spellsContainer.AddToClassList("VerticalItemsContainer");
            splitter.Add(spellsContainer);
            
            // Create selected spell pane
            m_SpellForm = new Form(OnCastSpell, "Cast", false);
            splitter.Add(m_SpellForm);
            Add(splitter);
            

            CreateSpellsGUI();
        }
        public override async UniTask OnPanelAttachedAsync(CancellationSignal cancellationSignal)
        {
            /* Subscribe to active spell type */
            using var activeSpellTypeSubscription = RuntimeCore.Instance.WatchActiveSpellType(OnActiveSpellTypeChanged);
            /* Watch for active spell type */
            using var watchActiveSpellType = RuntimeCore.Instance.OnSwitchActiveSpellType(OnSwitchActiveSpellType);
            /* Watch for selected objects */
            using var watchSelectedObjects = RuntimeCore.Instance.WatchSelectObjectsChanged(Revalidate);
            
            await cancellationSignal;
        }

        public override Preferences GetDefaultPreferences()
        {
            return new Preferences(new UnityEngine.Vector2(0, 0), new UnityEngine.Vector2(600, 0), new UnityEngine.Vector2(400, 0), new UnityEngine.Vector2(0, 0));
        }

        private void CreateSpellsGUI()
        {
            var allSpells = RuntimeCore.Instance.spellMetadata;
            foreach (var spell in allSpells)
            {
                m_ItemTemplate.CloneTree(m_SpellsContainer);
                var item = m_SpellsContainer.Query<VisualElement>("Item").Last();
                m_MapSpellTypeToElement[spell.SpellType] = item;
                
                var title = item.Query<Label>("Title").First();
                title.text = spell.Name;
                
                /* Capture click */
                void ClickHandler(MouseUpEvent evt)
                {
                    RuntimeCore.Instance.SetActiveSpellType(spell.SpellType);
                }
                item.RegisterCallback<MouseUpEvent>(ClickHandler);
            }
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
        
        private void Revalidate()
        {
            var selectedObjects = RuntimeCore.Instance.selectedObjects;
            if (selectedObjects.Count > 0)
            {
                if (m_CurrentEditor != null)
                {
                    var spell = m_CurrentEditor.Submit() as ISpell;
                    if (spell != null)
                    {
                        foreach (var selectedObject in selectedObjects)
                        {
                            var exception = spell.CanCast(God.Instance, selectedObject);
                            if (exception != null)
                            {
                                m_SpellForm.isValid = false;
                                m_SpellForm.SetMessage(exception.Message);
                                return;
                            }
                        }
                        m_SpellForm.isValid = true;
                        m_SpellForm.SetMessage("");
                        return;
                    }
                    else
                    {
                        m_SpellForm.isValid = false;
                        m_SpellForm.SetMessage("No spell selected");
                        return;
                    }
                }
            }
            m_SpellForm.isValid = false;
            m_SpellForm.SetMessage("No object selected");
        }

        private async UniTaskVoid OnSwitchActiveSpellType(Type spellType, CancellationSignal signal)
        {
            if (spellType == null)
            {
                m_SpellForm.ClearContent();
                m_SpellForm.isValid = false;
                m_SpellForm.SetMessage("No spell selected");
                Revalidate();
                return;
            }
            
            m_ActiveSpellType = spellType;
            m_CurrentEditor = new EditorFormElement(spellType);
            m_SpellForm.ClearContent();
            m_SpellForm.AddContent(m_CurrentEditor);
            Revalidate();
            await signal;
            m_SpellForm.ClearContent();
            m_SpellForm.isValid = true;
            Revalidate();
        }
        
        private void OnCastSpell()
        {
            Revalidate();
            if (m_SpellForm?.isValid == false)
            {
                return;
            }
            var spell = m_CurrentEditor.Submit() as ISpell;
            if (spell is IAsyncSpell asyncSpell)
            {
                foreach (var selectedObject in RuntimeCore.Instance.selectedObjects)
                {
                    asyncSpell.OnCast(God.Instance, selectedObject, CancellationSignal.None).Forget();
                }
            } else if (spell is ISyncSpell syncSpell)
            {
                foreach (var selectedObject in RuntimeCore.Instance.selectedObjects)
                {
                    syncSpell.OnCast(God.Instance, selectedObject);
                }
            }
        }
    }
}