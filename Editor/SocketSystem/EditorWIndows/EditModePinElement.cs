using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
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
            
            /* Create pin view */
            /* Unwatch button */
            var unwatchButton = new Button();
            unwatchButton.text = "Unwatch";
            unwatchButton.clicked += () =>
            {
                Pin.RemoveFromInspect(pin);
                unwatchButton.SetEnabled(false);
            };
            
            var pinView = SocketSystemEditorUtils.CreatePinView(typeof(Pin), pin, pin == null ? Color.black : pin.debugColor, null, new []
                {
                    (VisualElement) unwatchButton
                });
            
            container.Add(pinView);
        }
    }
}