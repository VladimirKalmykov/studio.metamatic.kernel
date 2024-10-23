using System;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Metamatic.Framework.Workshop;
using Metamatic.UIToolKit;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic.Framework.Widgets
{
    [MetamaticWidget("Metamatic/Entity", null, WidgetApplicability.Both)]
    public class EntityEditorWidget: Widget
    {
        private Tabs<EntityComponent> m_EntityComponentsTabs;
        
        public EntityEditorWidget()
        {
            styleSheets.Add(UIToolKitCore.styleSheet);
            styleSheets.Add(UIToolKitCore.tailwindStyleSheet);
            AddToClassList(UIToolKitCore.Tailwind.Column);
            
            // Add columns to the widget
            m_EntityComponentsTabs = new Tabs<EntityComponent>();
            Add(m_EntityComponentsTabs);
        }
        public override async UniTask OnPanelAttachedAsync(CancellationSignal cancellationSignal)
        {
            using var watchCharacterAIControllers = RuntimeCore.Instance.WatchEverySelectedObjectLifecycle(OnSelectedObjectChanged, TakeFirst);
            using var watchActiveTab = m_EntityComponentsTabs.ActiveTab.WatchLifecycle(OnActiveTabLifecycleChanged);
            await cancellationSignal;
        }
        
        private async UniTaskVoid OnActiveTabLifecycleChanged(EntityComponent entityComponent, CancellationSignal signal)
        {
            m_EntityComponentsTabs.Clear();
            if (entityComponent == null)
            {
                m_EntityComponentsTabs.Add(new Label($"Entity component is not selected"));
                return;
            }

            if (entityComponent.stateType == null)
            {
                m_EntityComponentsTabs.Add(new Label($"{entityComponent.GetType().Name} has no state"));
                return;
            }
            Type entityType = entityComponent.GetType();
            // Получаем свойство "state"
            PropertyInfo stateProperty = entityType.GetProperty("state");
            if (stateProperty == null)
            {
                m_EntityComponentsTabs.Add(new Label($"{entityComponent.GetType().Name} has no state property"));
                return;
            }
            var editor = new StructEditorFormElement(entityComponent.stateType, () => stateProperty.GetValue(entityComponent), (o => stateProperty.SetValue(entityComponent, o)));
            editor.AddToClassList(UIToolKitCore.Tailwind.Padding12);
            m_EntityComponentsTabs.Add(editor);
            await signal;
        }
        
        private static bool TakeFirst(GameObject gameObject, int index)
        {
            return index == 0;
        }
        
        private async UniTaskVoid OnSelectedObjectChanged(GameObject selectedObject, int index, CancellationSignal signal)
        {
            // Get Entity
            var entity = selectedObject.GetComponent<Entity>();
            if (entity == null) return;
            // Subscribe to entity components
            using var entityComponentsSubscription = entity.Components.WatchEachLifecycle(OnEntityComponentsChanged);
            await signal;
        }
        
        private async UniTaskVoid OnEntityComponentsChanged(EntityComponent entityComponent, int index, CancellationSignal signal)
        {
            using var tag = m_EntityComponentsTabs.AddTab(entityComponent.GetType().Name, entityComponent);
            await signal;
        }

        public override Preferences GetDefaultPreferences()
        {
            return new Preferences(new Vector2(200, 200), new Vector2(400, 0), new Vector2(300, 40), new Vector2(800, 800));
        }
    }
}