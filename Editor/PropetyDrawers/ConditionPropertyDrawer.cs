// ConditionDrawer.cs
#if UNITY_EDITOR
namespace Metamatic
{
    using UnityEditor;
    using UnityEngine;
    using System;
    using System.Linq;
    using System.Reflection;

    [CustomPropertyDrawer(typeof(ConditionLegacy))]
    public class ConditionDrawer : PropertyDrawer
    {
        const float PADDING = 2f;

        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            var targetProp = prop.FindPropertyRelative("target");
            var memberProp = prop.FindPropertyRelative("memberName");
            var argProp    = prop.FindPropertyRelative("stringArg");
            var invProp    = prop.FindPropertyRelative("invert");  // ← сериализованное поле

            EditorGUI.BeginProperty(pos, label, prop);

            // Foldout‑заголовок
            var headerRect = new Rect(pos.x, pos.y, pos.width, EditorGUIUtility.singleLineHeight);
            prop.isExpanded = EditorGUI.Foldout(headerRect, prop.isExpanded, label, true);

            if (prop.isExpanded)
            {
                // считаем, сколько полей рисовать
                bool hasStringArg = HasStringParameter(targetProp.objectReferenceValue, memberProp.stringValue);
                int lines = 2 // target + member
                          + (hasStringArg ? 1 : 0)
                          + 1;    // invert always
                float totalH = lines * EditorGUIUtility.singleLineHeight + (lines + 1) * PADDING;

                // фон
                float y = headerRect.yMax + PADDING;
                var boxRect = new Rect(pos.x, y, pos.width, totalH);
                EditorGUI.HelpBox(boxRect, GUIContent.none.text, MessageType.None);

                // поля без лейблов
                y += PADDING;
                float fieldW = pos.width - 4;
                float fieldX = pos.x + 2;

                // 1) target
                EditorGUI.PropertyField(new Rect(fieldX, y, fieldW, EditorGUIUtility.singleLineHeight), targetProp, GUIContent.none);
                y += EditorGUIUtility.singleLineHeight + PADDING;

                // 2) member popup
                string[] options = Array.Empty<string>();
                int selected = -1;
                if (targetProp.objectReferenceValue != null)
                {
                    var ti = targetProp.objectReferenceValue.GetType();
                    var methods = ti.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                    .Where(m => m.ReturnType == typeof(bool) &&
                                           (m.GetParameters().Length == 0 || m.GetParameters()[0].ParameterType == typeof(string)))
                                    .Select(m => m.Name);
                    var props = ti.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                  .Where(p => p.PropertyType == typeof(bool) && p.GetGetMethod(true) != null)
                                  .Select(p => p.Name);
                    options = methods.Concat(props).Distinct().OrderBy(n => n).ToArray();
                    selected = Array.IndexOf(options, memberProp.stringValue);
                }
                var popupRect = new Rect(fieldX, y, fieldW, EditorGUIUtility.singleLineHeight);
                if (options.Length > 0)
                {
                    int newSel = EditorGUI.Popup(popupRect, selected, options);
                    if (newSel != selected && newSel >= 0)
                        memberProp.stringValue = options[newSel];
                }
                else
                {
                    EditorGUI.LabelField(popupRect, "<no bool members>");
                    memberProp.stringValue = "";
                }
                y += EditorGUIUtility.singleLineHeight + PADDING;

                // 3) stringArg, если нужно
                if (hasStringArg)
                {
                    EditorGUI.PropertyField(new Rect(fieldX, y, fieldW, EditorGUIUtility.singleLineHeight), argProp, GUIContent.none);
                    y += EditorGUIUtility.singleLineHeight + PADDING;
                }

                // 4) invert‑чекбокс
                EditorGUI.PropertyField(new Rect(fieldX, y, fieldW, EditorGUIUtility.singleLineHeight),
                                        invProp, new GUIContent("Invert"));
            }
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
        {
            float h = EditorGUIUtility.singleLineHeight;
            if (prop.isExpanded)
            {
                var targetProp = prop.FindPropertyRelative("target");
                var memberProp = prop.FindPropertyRelative("memberName");
                bool hasStringArg = HasStringParameter(targetProp.objectReferenceValue, memberProp.stringValue);
                int lines = 2 + (hasStringArg ? 1 : 0) + 1;
                h += lines * EditorGUIUtility.singleLineHeight + (lines + 1) * PADDING;
            }
            return h;
        }

        private bool HasStringParameter(UnityEngine.Object obj, string methodName)
        {
            if (obj == null || string.IsNullOrEmpty(methodName)) return false;
            var mi = obj.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return mi != null
                && mi.ReturnType == typeof(bool)
                && mi.GetParameters().Length == 1
                && mi.GetParameters()[0].ParameterType == typeof(string);
        }
    }
}
#endif
