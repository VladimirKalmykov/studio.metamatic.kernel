using System;
using UnityEditor;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Metamatic
{
    public class GenericObjectField<T> : VisualElement
        where T : UnityEngine.Object
    {
        /* Markup */
        private bool m_ReadOnly;
        private VisualElement m_Display;
        private Label m_Label;
        private VisualElement m_FuncBtn;

        private Type m_Type;
        private UnityEngine.Object m_Value;
        
        public UnityEngine.Object value
        {
            get => m_Value;
            set
            {
                m_Value = value;
                UpdateValueView();
            }
        }
        public bool readOnly
        {
            get => m_ReadOnly;
            set
            {
                m_ReadOnly = value;
            }
        }

        public GenericObjectField(Type type, bool readOnly = false): base()
        {
            m_Type = type;
            m_ReadOnly = readOnly;
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/studio.metamatic.kernel/Editor/Components/GenericObjectField/GenericObjectField.uxml");
            visualTree.CloneTree(this);
            AddToClassList("GenericObjectField");
            
            m_Display = this.Q<VisualElement>("display");
            m_Label = this.Q<Label>("label");
            m_FuncBtn = this.Q<VisualElement>("func-btn");
            m_FuncBtn.RegisterCallback<MouseUpEvent>(evt =>
            {
                ShowContextMenu(m_FuncBtn);
            });
            
            m_Display.RegisterCallback<MouseUpEvent>(evt =>
            {
                var obj = value;
                if (obj != null)
                {
                    EditorGUIUtility.PingObject(obj);
                    var pin = value as Pin;
                    if (pin != null)
                    {
                        pin.DebugHighlight();
                    }
                }
            });
            
            /* Register mouseover и mouseleave */
            RegisterCallback<MouseOverEvent>(e => 
            {
                AddToClassList("Hover");
            });
            
            RegisterCallback<MouseLeaveEvent>(e => 
            {
                RemoveFromClassList("Hover");
            });
            
            /* DnD */
            RegisterCallback<DragUpdatedEvent>(evt =>
            {
                if (DragAndDrop.objectReferences.Length > 0)
                {
                    var dndObjectRef = DragAndDrop.objectReferences[0];
                    if (m_Type.IsAssignableFrom(dndObjectRef.GetType()))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                        AddToClassList("Droppable");
                    }
                    else
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    }
                }
                else
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                }
                evt.StopPropagation();
            });
            
            RegisterCallback<DragLeaveEvent>(evt =>
            {
                RemoveFromClassList("Droppable");
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            });

            RegisterCallback<DragPerformEvent>(evt =>
            {
                RemoveFromClassList("Droppable");
                if (DragAndDrop.objectReferences.Length > 0)
                {
                    var dndObjectRef = DragAndDrop.objectReferences[0];
                    if (m_Type.IsAssignableFrom(dndObjectRef.GetType()))
                    {
                        ChangeValue(dndObjectRef);
                    }
                }

                evt.StopPropagation();
            });

            UpdateValueView();
            UpdateReadonlyView();
        }
        
        private void ShowContextMenu(VisualElement targetElement)
        {
            var menu = new GenericDropdownMenu();

            menu.AddItem("Clear", false, ClearAction);
            
            #if UNITY_6000
            menu.DropDown(targetElement.worldBound, targetElement, false);
            #else
            menu.DropDown(targetElement.worldBound, targetElement);
            #endif
        }
        
        private void ClearAction()
        {
            ChangeValue(null);
        }
        
        private void UpdateValueView()
        {
            m_Label.text = m_Value == null ? MiscUtils.GetGenericName(m_Type) : m_Value.ToString();
            if (m_Value != null)
            {
                m_Label.AddToClassList("HasValue");
            }
            else
            {
                m_Label.RemoveFromClassList("HasValue");
            }
        }
        
        private void UpdateReadonlyView()
        {
            if (m_ReadOnly)
            {
                AddToClassList("ReadOnly");
            }
            else
            {
                RemoveFromClassList("ReadOnly");
            }
        }

        private void ChangeValue(UnityEngine.Object newValue)
        {
            if (newValue == m_Value) return;
            var prevValue = m_Value;
            value = newValue;
            
            if (panel != null)
            {
                using (ChangeEvent<UnityEngine.Object> pooled = ChangeEvent<UnityEngine.Object>.GetPooled(prevValue, newValue))
                {
                    pooled.target = (IEventHandler) this;
                    this.SendEvent((EventBase) pooled);
                }
            }
        }
    }
    
    public class GenericObjectField : GenericObjectField<Object>
    {
        public GenericObjectField(Type type, bool readOnly = false) : base(type, readOnly)
        {
        }
    }
}