using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic.Framework.Editor
{
    public class IconVisualElement: VisualElement
    {
        public IconVisualElement(string name, string size, Texture2D texture)
        {
            AddToClassList("Icon");
            AddToClassList($"Size{size}");
            
            var image = new Image();
            image.AddToClassList("Image");
            image.image = texture;
            
            Add(image);
            
            var label = new Label(name);
            label.AddToClassList("Label");
            
            Add(label);
        }
    }
}