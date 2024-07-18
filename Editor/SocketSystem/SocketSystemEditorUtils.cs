using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic
{
    public static class SocketSystemEditorUtils
    {
        /// <summary>
        /// Create pin view for the given type and pin object.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="pinObject"></param>
        /// <param name="color"></param>
        /// <param name="onChange"></param>
        /// <returns></returns>
        public static VisualElement CreatePinView(Type type, UnityEngine.Object pinObject, Color color, EventCallback<ChangeEvent<UnityEngine.Object>> onChange = null, VisualElement[] additionalElements = null)
        {
            /* Create pin view */
            var pinView = new VisualElement();
            pinView.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/studio.metamatic.kernel/Editor/SocketSystem/styles.uss"));
            pinView.AddToClassList("Pin");
                
            /* Create pin color */
            var pinColorContainer = new VisualElement();
            pinColorContainer.AddToClassList("PinColorContainer");
            var pinColor = new VisualElement();
            pinColor.AddToClassList("PinColor");
            pinColor.style.backgroundColor = color;
            pinColorContainer.Add(pinColor);
            pinView.Add(pinColorContainer);
                
            /* Object field container */
            var objectFieldContainer = new VisualElement();
            objectFieldContainer.AddToClassList("ObjectFieldContainer");
                
            var objectField = new GenericObjectField<Pin>(type, onChange == null)
            {
                value = pinObject
            };

            
                
            objectFieldContainer.Add(objectField);
            if (onChange != null)
            {
                objectField.RegisterCallback(onChange);
            }
            
            pinView.Add(objectFieldContainer);
            
            /* Add additional elements */
            if (additionalElements != null)
            {
                foreach (var element in additionalElements)
                {
                    pinView.Add(element);
                }
            }

            return pinView;
        }
    }
}