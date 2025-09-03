// ThemeEditor.cs
// -------------------------------------------------
#if UNITY_EDITOR
namespace Metamatic
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(Theme))]
    public class ThemeEditor : UnityEditor.Editor
    {
        private SerializedProperty _schemaProp;
        private SerializedProperty _slotGuids;
        private SerializedProperty _colors;
        private SerializedProperty _blocks;

        private string _search = string.Empty;
        private bool _showColors = true;
        private bool _showBlocks = true;

        private void OnEnable()
        {
            _schemaProp = serializedObject.FindProperty("schema");
            _slotGuids  = serializedObject.FindProperty("_slotGuids");
            _colors     = serializedObject.FindProperty("_colors");
            _blocks     = serializedObject.FindProperty("_blocks");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Поле схемы: сразу применяем и синкаем
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_schemaProp, new GUIContent("Schema"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                ((Theme)target).ForceSync();
                serializedObject.Update();
            }

            var theme  = (Theme)target;
            var schema = theme.schema;

            using (new EditorGUI.DisabledScope(schema == null))
            {
                EditorGUILayout.Space(4);
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    if (GUILayout.Button("Sync",     EditorStyles.toolbarButton, GUILayout.Width(60)))
                    {
                        theme.ForceSync();
                        serializedObject.Update();
                    }
                    if (GUILayout.Button("Validate", EditorStyles.toolbarButton, GUILayout.Width(70)))
                    {
                        Validate(schema);
                    }
                    GUILayout.FlexibleSpace();
                    _showColors = GUILayout.Toggle(_showColors, "Colors", EditorStyles.toolbarButton);
                    _showBlocks = GUILayout.Toggle(_showBlocks, "Blocks", EditorStyles.toolbarButton);
                    GUILayout.Space(8);
                    _search = GUILayout.TextField(
                        _search,
                        GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.toolbarTextField,
                        GUILayout.MinWidth(140)
                    );
                    if (GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(20))) _search = string.Empty;
                }
            }

            if (!schema)
            {
                EditorGUILayout.HelpBox("Assign a ThemeSchema to edit values.", MessageType.Info);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EnsureSerializedSizesMatch(schema);

            EditorGUILayout.Space(6);
            DrawKeys(schema);

            serializedObject.ApplyModifiedProperties();
        }

        private void EnsureSerializedSizesMatch(ThemeSchema schema)
        {
            int n = schema.keys.Count;
            if (_slotGuids.arraySize != n || _colors.arraySize != n || _blocks.arraySize != n)
            {
                ((Theme)target).ForceSync();
                serializedObject.Update();
            }
        }

        private void DrawKeys(ThemeSchema schema)
        {
            var searchLC = string.IsNullOrEmpty(_search) ? null : _search.ToLowerInvariant();

            // Без столбца Kind чтобы сэкономить место
            const float nameW = 260f;

            for (int i = 0; i < schema.keys.Count; i++)
            {
                var key = schema.keys[i];
                if (searchLC != null && !(key.name ?? string.Empty).ToLowerInvariant().Contains(searchLC))
                    continue;

                // Вычисляем высоту в зависимости от типа
                float valueHeight;
                if (key.kind == ThemeValueKind.Color)
                {
                    var colProp = _colors.GetArrayElementAtIndex(i);
                    valueHeight = EditorGUI.GetPropertyHeight(colProp, GUIContent.none, false);
                }
                else // ColorBlock
                {
                    var blkProp = _blocks.GetArrayElementAtIndex(i);
                    valueHeight = EditorGUI.GetPropertyHeight(blkProp, includeChildren: true);
                }

                var totalHeight = Mathf.Max(EditorGUIUtility.singleLineHeight, valueHeight) + 2f;
                var row = EditorGUILayout.GetControlRect(false, totalHeight);

                // Имя слева, значение справа
                var rName = new Rect(row.x, row.y, nameW, EditorGUIUtility.singleLineHeight);
                var rVal  = new Rect(rName.xMax + 6, row.y, row.width - nameW - 6, valueHeight);

                // Кликабельное имя: копируем в буфер
                var nameContent = new GUIContent(key.name, "Click to copy key name");
                EditorGUI.LabelField(rName, nameContent);
                EditorGUIUtility.AddCursorRect(rName, MouseCursor.Arrow);
                var evt = Event.current;
                if (evt.type == EventType.MouseDown && evt.button == 0 && rName.Contains(evt.mousePosition))
                {
                    EditorGUIUtility.systemCopyBuffer = key.name ?? string.Empty;
                    EditorWindow.focusedWindow?.ShowNotification(new GUIContent($"Copied: {key.name}"));
                    evt.Use();
                }

                // Значение
                switch (key.kind)
                {
                    case ThemeValueKind.Color:
                    {
                        if (!_showColors) continue;
                        var colProp = _colors.GetArrayElementAtIndex(i);
                        if (colProp != null)
                            EditorGUI.PropertyField(rVal, colProp, GUIContent.none, includeChildren: false);
                        break;
                    }
                    case ThemeValueKind.ColorBlock:
                    {
                        if (!_showBlocks) continue;
                        var blkProp = _blocks.GetArrayElementAtIndex(i);
                        if (blkProp != null)
                            EditorGUI.PropertyField(rVal, blkProp, GUIContent.none, includeChildren: true);
                        break;
                    }
                }
            }
        }

        private void Validate(ThemeSchema schema)
        {
            var issues = new List<string>();
            if (schema.keys.Count == 0) issues.Add("Schema has no keys.");
            if (issues.Count == 0)
                EditorUtility.DisplayDialog("Theme Validate", "No obvious issues.", "OK");
            else
                EditorUtility.DisplayDialog("Theme Validate", string.Join("\n", issues), "OK");
        }
    }
}
#endif
