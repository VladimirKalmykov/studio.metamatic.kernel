using System;

namespace Metamatic
{
    #if UNITY_EDITOR
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;

    [CustomEditor(typeof(ThemeSchema))]
    public class ThemeSchemaEditor : UnityEditor.Editor
    {
        private ReorderableList _list;
        private SerializedProperty _keysProp;
        private string _search = string.Empty;
        private GUIStyle _headerLabel;

        private void OnEnable()
        {
            _keysProp = serializedObject.FindProperty("keys");
            _list = new ReorderableList(serializedObject, _keysProp, true, true, true, true);
            _list.drawHeaderCallback = DrawHeader;
            _list.drawElementCallback = DrawElement;
            _list.elementHeight = EditorGUIUtility.singleLineHeight + 6;
            _list.onAddDropdownCallback = OnAddDropdown;
            _list.onCanRemoveCallback = CanRemove;
            _list.onRemoveCallback = OnRemove;
            _headerLabel = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleLeft };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Toolbar
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Add Color", EditorStyles.toolbarButton, GUILayout.Width(80)))
                    AddKey(ThemeValueKind.Color);
                if (GUILayout.Button("Add Block", EditorStyles.toolbarButton, GUILayout.Width(80)))
                    AddKey(ThemeValueKind.ColorBlock);
                GUILayout.Space(8);
                if (GUILayout.Button("Sort A→Z", EditorStyles.toolbarButton, GUILayout.Width(70))) SortByName();
                if (GUILayout.Button("Validate", EditorStyles.toolbarButton, GUILayout.Width(70))) ValidateNames();
                GUILayout.FlexibleSpace();
                _search = GUILayout.TextField(_search,
                    GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.toolbarTextField,
                    GUILayout.MinWidth(120));
                if (GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(20))) _search = string.Empty;
            }

            GUILayout.Space(4);
            _list.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader(Rect rect)
        {
            const float guidW = 230f;
            const float kindW = 90f;
            var nameW = rect.width - guidW - kindW - 14f;

            var rGuid = new Rect(rect.x + 6, rect.y, guidW, rect.height);
            var rName = new Rect(rGuid.xMax + 6, rect.y, nameW, rect.height);
            var rKind = new Rect(rName.xMax + 6, rect.y, kindW, rect.height);

            EditorGUI.LabelField(rGuid, "Guid", _headerLabel);
            EditorGUI.LabelField(rName, "Name", _headerLabel);
            EditorGUI.LabelField(rKind, "Kind", _headerLabel);
        }

        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var prop = _keysProp.GetArrayElementAtIndex(index);
            var guid = prop.FindPropertyRelative("guid");
            var name = prop.FindPropertyRelative("name");
            var kind = prop.FindPropertyRelative("kind");

            if (!string.IsNullOrEmpty(_search))
            {
                var n = name.stringValue ?? string.Empty;
                if (!n.ToLowerInvariant().Contains(_search.ToLowerInvariant()))
                    return; // skip draw; keeps layout compact
            }

            rect.y += 2;
            rect.height = EditorGUIUtility.singleLineHeight;
            const float guidW = 230f;
            const float kindW = 90f;
            var nameW = rect.width - guidW - kindW - 14f;

            var rGuid = new Rect(rect.x + 6, rect.y, guidW, rect.height);
            var rName = new Rect(rGuid.xMax + 6, rect.y, nameW, rect.height);
            var rKind = new Rect(rName.xMax + 6, rect.y, kindW, rect.height);

            using (new EditorGUI.DisabledScope(true))
                EditorGUI.TextField(rGuid, guid.stringValue);

            // Name with duplicate warning tint
            var dup = IsDuplicateName(name.stringValue, index);
            var bgc = GUI.backgroundColor;
            if (dup || string.IsNullOrWhiteSpace(name.stringValue))
                GUI.backgroundColor = dup ? new Color(1, .78f, .78f) : new Color(1f, .9f, .75f);
            name.stringValue = EditorGUI.DelayedTextField(rName, name.stringValue);
            GUI.backgroundColor = bgc;

            EditorGUI.PropertyField(rKind, kind, GUIContent.none);
        }

        private void OnAddDropdown(Rect buttonRect, ReorderableList list)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Color"), false, () => AddKey(ThemeValueKind.Color));
            menu.AddItem(new GUIContent("Color Block"), false, () => AddKey(ThemeValueKind.ColorBlock));
            menu.ShowAsContext();
        }

        private bool CanRemove(ReorderableList list)
        {
            return list.count > 0 && list.index >= 0;
        }

        private void OnRemove(ReorderableList list)
        {
            if (!CanRemove(list)) return;
            _keysProp.DeleteArrayElementAtIndex(list.index);
            serializedObject.ApplyModifiedProperties();
        }

        private void AddKey(ThemeValueKind kind)
        {
            serializedObject.Update();
            _keysProp.arraySize++;
            var p = _keysProp.GetArrayElementAtIndex(_keysProp.arraySize - 1);
            p.FindPropertyRelative("guid").stringValue = Guid.NewGuid().ToString();
            p.FindPropertyRelative("name").stringValue = SuggestUniqueName(kind);
            p.FindPropertyRelative("kind").enumValueIndex = (int)kind;
            serializedObject.ApplyModifiedProperties();
        }

        private void SortByName()
        {
            serializedObject.Update();
            var list = new List<SerializedProperty>();
            for (int i = 0; i < _keysProp.arraySize; i++) list.Add(_keysProp.GetArrayElementAtIndex(i).Copy());
            list.Sort((a, b) => string.Compare(a.FindPropertyRelative("name").stringValue,
                b.FindPropertyRelative("name").stringValue, StringComparison.Ordinal));
            // Rewrite
            var tmp = new List<(string guid, string name, int kind)>();
            foreach (var sp in list)
                tmp.Add((sp.FindPropertyRelative("guid").stringValue, sp.FindPropertyRelative("name").stringValue,
                    sp.FindPropertyRelative("kind").enumValueIndex));
            for (int i = 0; i < tmp.Count; i++)
            {
                var e = _keysProp.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("guid").stringValue = tmp[i].guid;
                e.FindPropertyRelative("name").stringValue = tmp[i].name;
                e.FindPropertyRelative("kind").enumValueIndex = tmp[i].kind;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void ValidateNames()
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < _keysProp.arraySize; i++)
            {
                var name = _keysProp.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue;
                if (string.IsNullOrWhiteSpace(name))
                {
                    EditorUtility.DisplayDialog("Validation", $"Key #{i + 1} has empty name.", "OK");
                    return;
                }

                if (!seen.Add(name))
                {
                    EditorUtility.DisplayDialog("Validation", $"Duplicate name: '{name}'.", "OK");
                    return;
                }
            }

            EditorUtility.DisplayDialog("Validation", "All key names look unique.", "Nice");
        }

        private bool IsDuplicateName(string name, int selfIndex)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            for (int i = 0; i < _keysProp.arraySize; i++)
            {
                if (i == selfIndex) continue;
                if (_keysProp.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name)
                    return true;
            }

            return false;
        }

        private string SuggestUniqueName(ThemeValueKind kind)
        {
            string baseName = kind == ThemeValueKind.Color ? "color_" : "block_";
            int n = 1;
            while (true)
            {
                string candidate = baseName + n;
                bool exists = false;
                for (int i = 0; i < _keysProp.arraySize; i++)
                {
                    if (_keysProp.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == candidate)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists) return candidate;
                n++;
            }
        }
    }

#endif

}