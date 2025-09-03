#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Metamatic
{
    [CustomEditor(typeof(ThemeBinding))]
    public class ThemeBindingUniversalEditor : UnityEditor.Editor
    {
        private SerializedProperty _schemaProp;   // поле из ThemeConsumerBase
        private SerializedProperty _bindings;

        private void OnEnable()
        {
            _schemaProp = serializedObject.FindProperty("schema");   // из базового класса
            _bindings   = serializedObject.FindProperty("bindings");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 1) Schema: можно редактировать
            EditorGUILayout.PropertyField(_schemaProp, new GUIContent("Schema"));

            // 2) Resolved Theme (readonly)
            using (new EditorGUI.DisabledScope(true))
            {
                var resolved = ResolveThemeForEditor((ThemeBinding)target);
                EditorGUILayout.ObjectField("Resolved Theme", resolved, typeof(ThemeBase), false);
            }

            EditorGUILayout.Space(6);

            var schema = _schemaProp.objectReferenceValue as ThemeSchema;
            if (!schema)
            {
                EditorGUILayout.HelpBox("Assign Schema to edit bindings.", MessageType.Info);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            // Биндинги
            for (int i = 0; i < _bindings.arraySize; i++)
            {
                var b = _bindings.GetArrayElementAtIndex(i);
                DrawBinding(b, schema, i);
                EditorGUILayout.Space(2);
            }

            if (GUILayout.Button("+ Add Binding"))
                _bindings.InsertArrayElementAtIndex(_bindings.arraySize);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBinding(SerializedProperty b, ThemeSchema schema, int index)
        {
            var key        = b.FindPropertyRelative("key");
            var target     = b.FindPropertyRelative("target");

            var graphic    = b.FindPropertyRelative("graphic");
            var selectable = b.FindPropertyRelative("selectable");
#if TMP_PRESENT
            var tmpText    = b.FindPropertyRelative("tmpText");
#endif
            var material   = b.FindPropertyRelative("material");
            var matProp    = b.FindPropertyRelative("matProperty");
            var component  = b.FindPropertyRelative("component");
            var member     = b.FindPropertyRelative("memberName");
            var onColor    = b.FindPropertyRelative("onColor");
            var onString   = b.FindPropertyRelative("onString");

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"Binding #{index + 1}", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Auto-detect", GUILayout.Width(100)))
                        AutoDetectTarget(b);
                    if (GUILayout.Button("X", GUILayout.Width(22)))
                    {
                        _bindings.DeleteArrayElementAtIndex(index);
                        return;
                    }
                }

                // Ключ под нашу Schema
                DrawThemeKeyRef(key, schema);

                EditorGUILayout.PropertyField(target);

                var tk = (ThemeBinding.TargetKind)target.enumValueIndex;
                switch (tk)
                {
                    case ThemeBinding.TargetKind.Auto:
                        EditorGUILayout.HelpBox("Auto: выберется по первым найденным целям.", MessageType.None);
                        break;

                    case ThemeBinding.TargetKind.GraphicColor:
                        EditorGUILayout.PropertyField(graphic);
                        break;

                    case ThemeBinding.TargetKind.SelectableColors:
                        EditorGUILayout.PropertyField(selectable);
                        break;

#if TMP_PRESENT
                    case ThemeBindingUniversal.TargetKind.TMPTextColor:
                        EditorGUILayout.PropertyField(tmpText);
                        break;
#endif
                    case ThemeBinding.TargetKind.MaterialColorProperty:
                        EditorGUILayout.PropertyField(material);
                        EditorGUILayout.PropertyField(matProp);
                        break;

                    case ThemeBinding.TargetKind.FieldOrProperty:
                        EditorGUILayout.PropertyField(component);
                        EditorGUILayout.PropertyField(member);
                        break;

                    case ThemeBinding.TargetKind.UnityEventColor:
                        EditorGUILayout.PropertyField(onColor);
                        break;

                    case ThemeBinding.TargetKind.UnityEventString:
                        EditorGUILayout.PropertyField(onString);
                        break;
                }
            }
        }

        private void DrawThemeKeyRef(SerializedProperty keyProp, ThemeSchema schema)
        {
            var schemaProp = keyProp.FindPropertyRelative("schema");
            var guidProp   = keyProp.FindPropertyRelative("keyGuid"); // имя поля GUID в ThemeKeyRef

            // Проставляем схему в ключ, если пусто
            if (schema && schemaProp.objectReferenceValue == null)
                schemaProp.objectReferenceValue = schema;

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField("Schema", schemaProp.objectReferenceValue, typeof(ThemeSchema), false);

                var s = schemaProp.objectReferenceValue as ThemeSchema;
                if (!s)
                {
                    EditorGUILayout.HelpBox("Assign schema", MessageType.Warning);
                    return;
                }

                // Выпадающий список ключей
                var names = new System.Collections.Generic.List<string>();
                var guids = new System.Collections.Generic.List<string>();
                foreach (var k in s.keys) { names.Add(k.name); guids.Add(k.guid); }

                int cur = Mathf.Max(0, guids.IndexOf(guidProp.stringValue));
                int next = EditorGUILayout.Popup("Key", cur, names.ToArray());
                if (next != cur) guidProp.stringValue = guids[next];
            }
        }

        private void AutoDetectTarget(SerializedProperty b)
        {
            var target     = b.FindPropertyRelative("target");
            var graphic    = b.FindPropertyRelative("graphic");
            var selectable = b.FindPropertyRelative("selectable");
#if TMP_PRESENT
            var tmpText    = b.FindPropertyRelative("tmpText");
#endif
            var material   = b.FindPropertyRelative("material");
            var component  = b.FindPropertyRelative("component");
            var member     = b.FindPropertyRelative("memberName");

            if (selectable.objectReferenceValue) target.enumValueIndex = (int)ThemeBinding.TargetKind.SelectableColors;
            else if (graphic.objectReferenceValue) target.enumValueIndex = (int)ThemeBinding.TargetKind.GraphicColor;
#if TMP_PRESENT
            else if (tmpText.objectReferenceValue) target.enumValueIndex = (int)ThemeBindingUniversal.TargetKind.TMPTextColor;
#endif
            else if (material.objectReferenceValue) target.enumValueIndex = (int)ThemeBinding.TargetKind.MaterialColorProperty;
            else if (component.objectReferenceValue && !string.IsNullOrEmpty(member.stringValue)) target.enumValueIndex = (int)ThemeBinding.TargetKind.FieldOrProperty;
            else target.enumValueIndex = (int)ThemeBinding.TargetKind.UnityEventColor;
        }

        // Пытаемся показать ту же тему, что и сам компонент использует в редакторе
        private ThemeBase ResolveThemeForEditor(ThemeBinding comp)
        {
            // 1) сначала пробуем взять защищённое поле currentTheme рефлексией
            var f = typeof(ThemeConsumer).GetField("currentTheme", BindingFlags.Instance | BindingFlags.NonPublic);
            var cur = f?.GetValue(comp) as ThemeBase;
            if (cur) return cur;

            // 2) повторяем логику EditorResolve из ThemeConsumerBase
            var schema = _schemaProp.objectReferenceValue as ThemeSchema;
            if (!schema) return null;

            var svc = Object.FindFirstObjectByType<ThemeService>();
            if (svc != null)
            {
#if UNITY_EDITOR
                foreach (var e in svc.entries)
                    if (e.schema == schema && e.editorTheme) return e.editorTheme;
#endif
            }

#if UNITY_EDITOR
            var guids = AssetDatabase.FindAssets("t:Theme");
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var theme = AssetDatabase.LoadAssetAtPath<Theme>(path);
                if (theme && theme.schema == schema) return theme;
            }
#endif
            return null;
        }
    }
}
#endif
