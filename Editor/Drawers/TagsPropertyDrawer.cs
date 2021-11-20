using UnityEditor;
 using UnityEngine;
 
 namespace Metamatic {
    [CustomPropertyDrawer(typeof(Tags))]
    public class TagsPropertyDrawer : PropertyDrawer 
    {
        public override void OnGUI(Rect position, SerializedProperty _property, GUIContent _label)
        {
            EditorGUI.BeginProperty(position, GUIContent.none, _property);
            
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), _label);
            SerializedProperty itemsProperty = _property.FindPropertyRelative("names");
            int length = itemsProperty.arraySize;
            
            float popupHeight = EditorGUI.GetPropertyHeight(SerializedPropertyType.LayerMask, _label);
            float groupHeight = popupHeight * length;
            GUI.BeginGroup(new Rect(
                position.x,
                position.y,
                position.width,
                groupHeight
            ));
            
            Rect subPostition = new Rect(
                0, 
                0,
                position.width,
                popupHeight
            );
            float removeButtonWidth = 60f;

            for (int i = 0; i < itemsProperty.arraySize; i++) {
                itemsProperty.GetArrayElementAtIndex(i).stringValue = EditorGUI.TagField(new Rect(
                    subPostition.x,
                    subPostition.y,
                    subPostition.width - (removeButtonWidth + 1f),
                    subPostition.height
                ), itemsProperty.GetArrayElementAtIndex(i).stringValue);

                Rect buttonPosition = new Rect(
                    subPostition.x + (subPostition.width - removeButtonWidth) + 1f,
                    subPostition.y,
                    removeButtonWidth,
                    subPostition.height
                );
                if (GUI.Button(buttonPosition, new GUIContent("Remove"))) {
                    itemsProperty.DeleteArrayElementAtIndex(i);
                    i--;
                }

                subPostition = new Rect(
                    subPostition.x,
                    subPostition.y + EditorGUI.GetPropertyHeight(SerializedPropertyType.LayerMask, _label),
                    subPostition.width,
                    subPostition.height
                );
            }
            GUI.EndGroup();
            position.y += groupHeight;
            position.height -= groupHeight + 10f;
            position.y += EditorGUIUtility.standardVerticalSpacing;
            if (GUI.Button(position, "Add tag")) {
                itemsProperty.InsertArrayElementAtIndex(length);
            }
            EditorGUI.EndProperty( );
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty itemsProperty = property.FindPropertyRelative("names");
            int length = itemsProperty.arraySize;
            float height = 0f;
            height += EditorGUI.GetPropertyHeight(SerializedPropertyType.LayerMask, label) * length;
            /* Space */
            height += EditorGUIUtility.standardVerticalSpacing;
            /* Button */
            height += EditorGUIUtility.singleLineHeight;
            /* Space after */
            height += 10f;
            return height;
        }
    }
 }