using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Metamatic
{
    public class EditModePinElement: VisualElement
    {
        public readonly Pin pin;
        public EditModePinElement(Pin pin) : base()
        {
            this.pin = pin;
            
            /* Container */
            var container = new VisualElement();
            container.AddToClassList("EditModePinContainer");
            Add(container);
            
            /* Component selector */
            var componentSelector = new ObjectField();
            componentSelector.objectType = typeof(Pin);
            componentSelector.RegisterValueChangedCallback(evt =>
            {
                return;
            });
            componentSelector.value = pin;
            
            /* Unwatch button */
            var unwatchButton = new Button();
            unwatchButton.text = "Unwatch";
            unwatchButton.clicked += () =>
            {
                Pin.RemoveFromInspect(pin);
                unwatchButton.SetEnabled(false);
            };
            
            container.Add(componentSelector);
            container.Add(unwatchButton);
        }
    }
}