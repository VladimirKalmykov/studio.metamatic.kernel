using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Metamatic.UIElements;
using UnityEngine.UIElements;

namespace Metamatic.UIToolKit
{
    public class SwitchButton<T>: AsyncVisualElement
    {
        private readonly Dictionary<T, VisualElement> m_Choices = new Dictionary<T, VisualElement>();
        private Observable<T> m_Value;
        public SwitchButton()
        {
            m_Value = new Observable<T>();
            AddToClassList("MetamaticUIKit_ToggleButton");
        }

        public SwitchButton(Observable<T> observableValue)
        {
            m_Value = observableValue;
            AddToClassList("MetamaticUIKit_ToggleButton");
        }

        public void AddChoice(string choice, T value)
        {
            var choiceElement = new VisualElement();
            choiceElement.AddToClassList("MetamaticUIKit_ToggleButtonChoice");
            choiceElement.Add(new Label(choice));
            
            choiceElement.RegisterCallback<ClickEvent>(evt =>
            {
                m_Value.Value = value;
            });
            
            if (m_Value.Value.Equals(value))
            {
                choiceElement.AddToClassList("MetamaticUIKit_ToggleButtonChoiceSelected");
            }
            m_Choices.Add(value, choiceElement);
            hierarchy.Add(choiceElement);
        }

        public override async UniTask OnAttachedAsync(Signal detachSignal)
        {
            using var watchSelectedValue = m_Value.Watch(InternalOnValueChanged);
            await detachSignal;
        }
        
        private void InternalOnValueChanged(T value)
        {
            foreach (var choice in m_Choices)
            {
                choice.Value.RemoveFromClassList("MetamaticUIKit_ToggleButtonChoiceSelected");
            }

            if (m_Choices.TryGetValue(value, out var activeChoice))
            {
                activeChoice.AddToClassList("MetamaticUIKit_ToggleButtonChoiceSelected");
            }
        }
        
        public DisposableAction AddValueChangedCallback(System.Action<T> callback)
        {
            return m_Value.Watch(callback, true);
        }
    }
}