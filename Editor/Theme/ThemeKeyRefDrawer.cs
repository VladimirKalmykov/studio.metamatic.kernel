namespace Metamatic
{
    // --- ThemeKeyRefDrawer.cs ---
#if UNITY_EDITOR
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;


    [CustomPropertyDrawer(typeof(ThemeKeyRef))]
    public class ThemeKeyRefDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var schemaProp = property.FindPropertyRelative("schema");
            var keyGuidProp = property.FindPropertyRelative("keyGuid");


            var rSchema = new Rect(position.x, position.y, position.width * 0.5f, position.height);
            var rKey = new Rect(rSchema.xMax + 4, position.y, position.width - rSchema.width - 4, position.height);


            EditorGUI.PropertyField(rSchema, schemaProp, label);


            var schema = schemaProp.objectReferenceValue as ThemeSchema;
            if (!schema)
            {
                EditorGUI.LabelField(rKey, "<no schema>", EditorStyles.helpBox);
                return;
            }


// filter by [ThemeKind] if present
            ThemeValueKind? required = null;
            var kindAttr = fieldInfo.GetCustomAttributes(typeof(ThemeKindAttribute), true);
            if (kindAttr != null && kindAttr.Length > 0)
            {
                var tk = (ThemeKindAttribute)kindAttr[0];
                required = tk.kind;
            }


            var keys = schema.keys;
            var names = new List<string>();
            var guids = new List<string>();
            for (int i = 0; i < keys.Count; i++)
            {
                if (required.HasValue && keys[i].kind != required.Value) continue;
                names.Add(keys[i].name);
                guids.Add(keys[i].guid);
            }


            int currentIndex = Mathf.Max(0, guids.IndexOf(keyGuidProp.stringValue));
            if (names.Count == 0)
            {
                EditorGUI.LabelField(rKey, "<no keys>", EditorStyles.helpBox);
                return;
            }


            int next = EditorGUI.Popup(rKey, currentIndex, names.ToArray());
            if (next != currentIndex)
            {
                keyGuidProp.stringValue = guids[next];
            }
        }
    }
#endif
}