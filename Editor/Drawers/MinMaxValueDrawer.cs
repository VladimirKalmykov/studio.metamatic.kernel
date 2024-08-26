using UnityEditor;
using UnityEngine;

namespace Metamatic {
    [CustomPropertyDrawer(typeof(MinMaxValue), true)]
    public class MinMaxValueDrawer: PropertyDrawer {
        /// <summary>
        /// OnGUI is called for rendering and handling GUI events.
        /// This function can be called multiple times per frame (one call per event).
        /// </summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect prefixedPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            int mode = property.FindPropertyRelative("mode").enumValueIndex;

            if (mode == (int)MinMaxValue.Mode.Constant) {
                position = DrawConstantValue(prefixedPosition, property);
            } else if (mode == (int)MinMaxValue.Mode.Range) {
                position = DrawRangeValue(prefixedPosition, property);
            }

            EditorGUI.PropertyField(position, property.FindPropertyRelative("mode"), new GUIContent(""));
        }

        Rect DrawConstantValue(Rect position, SerializedProperty property) {
            property.FindPropertyRelative("constant").floatValue = EditorGUI.FloatField(
                new Rect(
                    position.x,
                    position.y,
                    position.width - 100,
                    position.height
                ),
                property.FindPropertyRelative("constant").floatValue
            );

            return new Rect(
                position.x + position.width - 90,
                position.y,
                90,
                position.height
            );
        }

        Rect DrawRangeValue(Rect position, SerializedProperty property) {
            property.FindPropertyRelative("constantMin").floatValue = EditorGUI.FloatField(
                new Rect(
                    position.x,
                    position.y,
                    (position.width - 100) / 2,
                    position.height
                ),
                property.FindPropertyRelative("constantMin").floatValue
            );

            property.FindPropertyRelative("constantMax").floatValue = EditorGUI.FloatField(
                new Rect(
                    position.x + ((position.width - 100) / 2) + 5,
                    position.y,
                    (position.width - 100) / 2,
                    position.height
                ),
                property.FindPropertyRelative("constantMax").floatValue
            );

            return new Rect(
                position.x + (position.width - 90),
                position.y,
                90,
                position.height
            );
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent _label) {
            return 20;
        }  
    }
}