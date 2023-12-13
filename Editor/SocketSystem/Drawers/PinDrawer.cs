using System.Linq;

namespace Metamatic
{
    using UnityEngine;
    using UnityEditor;
    
    [CustomPropertyDrawer(typeof(Pin))]
    public class PinDrawer : PropertyDrawer
    {
        private readonly Texture2D m_PinInputActive = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/studio.metamatic.kernel/Editor/SocketSystem/Drawers/assets/16/PinInputActive.png");
        private readonly Texture2D m_PinInputUnactive = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/studio.metamatic.kernel/Editor/SocketSystem/Drawers/assets/16/PinInputUnactive.png");
        private readonly Texture2D m_PinOutputActive = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/studio.metamatic.kernel/Editor/SocketSystem/Drawers/assets/16/PinOutputActive.png");
        private readonly Texture2D m_PinOutputUnactive = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/studio.metamatic.kernel/Editor/SocketSystem/Drawers/assets/16/PinOutputUnactive.png");
        private readonly Texture2D m_PinDisabled = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/studio.metamatic.kernel/Editor/SocketSystem/Drawers/assets/16/PinDisabled.png");
        private readonly Texture2D m_PinError = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/studio.metamatic.kernel/Editor/SocketSystem/Drawers/assets/16/PinError.png");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            Texture2D imageToShow = m_PinDisabled;
            var hasVirtualInput = false;
            var hasVirtualOutput = false;
            Pin pin = property.objectReferenceValue as Pin;
            if (pin != null)
            {
                if (pin.hasInputPin)
                {
                    if (pin.DebugGetInputPin() is VirtualPin)
                    {
                        hasVirtualInput = true;
                    }
                }

                if (pin.hasOutputPins)
                {
                    if (pin.DebugGetOutputPins().Any(item => item is VirtualPin))
                    {
                        hasVirtualOutput = true;
                    }
                }

                if (hasVirtualInput && hasVirtualOutput)
                {
                    imageToShow = m_PinError;
                } else if (hasVirtualInput)
                {
                    imageToShow = m_PinInputUnactive;
                } else if (hasVirtualOutput)
                {
                    imageToShow = m_PinOutputUnactive;
                }
            }
            else
            {
                imageToShow = m_PinDisabled;
            }

            // Отрисовка изображения
            Rect imageRect = new Rect(position.x, position.y + 1, 16, 16);
            GUI.DrawTexture(imageRect, imageToShow);

            // Отрисовка остальной части свойства
            Rect propertyRect = new Rect(position.x + 20, position.y, position.width - 20, position.height);
            EditorGUI.PropertyField(propertyRect, property, GUIContent.none);

            EditorGUI.EndProperty();
        }
    }
}