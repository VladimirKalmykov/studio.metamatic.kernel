using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic.Framework.Editor
{
    public class IconPackVisualElement: VisualElement
    {
        public IconPackVisualElement(string size, List<(string, Func<Texture2D>)> icons)
        {
            AddToClassList("IconPack");
            
            var header = new Label(size);
            header.AddToClassList("Header");
            
            Add(header);
            
            var content = new VisualElement();
            content.AddToClassList("Content");
            
            Add(content);
            
            foreach (var icon in icons)
            {
                var iconElement = new IconVisualElement(icon.Item1, size, icon.Item2());
                content.Add(iconElement);
            }
        }
    }
}