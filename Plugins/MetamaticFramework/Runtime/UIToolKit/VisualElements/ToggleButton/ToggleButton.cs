using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Metamatic.UIElements;
using UnityEngine.UIElements;

namespace Metamatic.UIToolKit
{
    public class ToggleButton<T>: AsyncVisualElement
    {
        private readonly Dictionary<T, VisualElement> m_Choices = new Dictionary<T, VisualElement>();
        private readonly Observable<T> m_SelectedValue = new Observable<T>();
        public ToggleButton()
        {
            AddToClassList("MetamaticUIKit_ToggleButton");
        }

        public void AddChoice(string choice, T value)
        {
            var choiceElement = new VisualElement();
            choiceElement.AddToClassList("MetamaticUIKit_ToggleButtonChoice");
        }

        public override async UniTask OnAttachedAsync(CancellationSignal detachSignal)
        {
            using var watchSelectedValue = m_SelectedValue.Watch(OnValueChanged);
            await detachSignal;
        }
        
        private void OnValueChanged(T value)
        {
            foreach (var choice in m_Choices)
            {
                choice.Value.RemoveFromClassList("MetamaticUIKit_ToggleButtonChoiceSelected");
            }

            m_Choices[value].AddToClassList("MetamaticUIKit_ToggleButtonChoiceSelected");
        }
    }
}