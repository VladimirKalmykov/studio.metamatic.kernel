using UnityEngine.UIElements;

namespace Metamatic.UIToolKit
{
    public class Field: VisualElement
    {
        public Field(string label)
        {
            this.style.flexDirection = FlexDirection.Column;
            // Create label at the top
            var labelElement = new Label(label);
            labelElement.style.marginBottom = 2;
            labelElement.RegisterCallback<MouseDownEvent>(evt =>
            {
                // Iterate over all children and find the first one that is not a label
                // and focus it
                foreach (var child in Children())
                {
                    if (child != labelElement)
                    {
                        child.Focus();
                        break;
                    }
                }
            });
            Add(labelElement);
        }
    }
}