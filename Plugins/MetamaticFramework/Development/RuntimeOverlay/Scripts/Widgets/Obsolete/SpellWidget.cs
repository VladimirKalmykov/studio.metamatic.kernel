using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = System.Object;

namespace Metamatic.Framework
{
    public class SpellWidget: WidgetLegacy
    {
        private PanelElement m_SpellPanel;
        private Form m_SpellForm;
        private Type m_ActiveSpellType;
        private List<string> m_Messages = new List<string>();
        private EditorFormElement m_CurrentEditor;
        
        public SpellWidget(Overlay overlay) : base(overlay)
        {
        }

        public override async UniTaskVoid OnInitializeWidget(CancellationSignal signal)
        {
            /* Create spell panel */
            m_SpellPanel = overlay.CreatePanel("panels/spells_widget/spell", "Spell", new PanelElement.Constraints()
            {
                DefaultPreferences = new PanelElement.UserPreferences()
                {
                    Position = new Vector2(200, 200),
                    Size = new Vector2(300, 0)  
                },
                MaxSize = new Vector2(500, 600),
                MinSize = new Vector2(300, 120),
                AutoHeight = true,
                AutoWidth = false
            });
            m_SpellForm = new Form(OnCastSpell, "Cast", false); // 123
            m_SpellPanel.AddContent(m_SpellForm);
            overlay.root.Add(m_SpellPanel);
            
            /* Watch for active spell type */
            using var watchActiveSpellType = overlay.core.OnSwitchActiveSpellType(OnSwitchActiveSpellType);
            
            /* Watch for selected objects */
            using var watchSelectedObjects = overlay.core.WatchSelectObjectsChanged(Revalidate);
            
            await signal;
            overlay.root.Remove(m_SpellPanel);
        }

        private void Revalidate()
        {
            var selectedObjects = overlay.core.selectedObjects;
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
            
            m_SpellPanel.title = spellType.Name;
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
                foreach (var selectedObject in overlay.core.selectedObjects)
                {
                    asyncSpell.OnCast(God.Instance, selectedObject, CancellationSignal.None).Forget();
                }
            } else if (spell is ISyncSpell syncSpell)
            {
                foreach (var selectedObject in overlay.core.selectedObjects)
                {
                    syncSpell.OnCast(God.Instance, selectedObject);
                }
            }
        }
    }
}