#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UI; // Graphic, Selectable
#if TMP_PRESENT
using TMPro;
#endif

namespace Metamatic.EditorUI
{
    [CustomEditor(typeof(ThemeBinding))]
    public class ThemeBindingInspector : UnityEditor.Editor
    {
        private SerializedProperty _schemaProp;
        private SerializedProperty _bindingsProp;

        public override VisualElement CreateInspectorGUI()
        {
            serializedObject.Update();

            _schemaProp = serializedObject.FindProperty("schema");
            _bindingsProp = serializedObject.FindProperty("bindings");

            var root = new VisualElement();
            root.style.paddingLeft = 6;
            root.style.paddingRight = 6;
            root.style.paddingTop = 6;
            root.style.paddingBottom = 6;
            root.style.flexDirection = FlexDirection.Column;

            static void AddWithSpace(VisualElement parent, VisualElement child, float space = 6)
            {
                if (parent.childCount > 0) child.style.marginTop = space;
                parent.Add(child);
            }

            // Шапка
            var schemaField = new PropertyField(_schemaProp, "Schema");
            AddWithSpace(root, schemaField);

            var resolvedThemeField = new ObjectField("Resolved Theme")
            {
                objectType = typeof(ThemeBase),
                value = ResolveThemeForEditor((ThemeBinding)target)
            };
            resolvedThemeField.SetEnabled(false);
            AddWithSpace(root, resolvedThemeField);

            schemaField.RegisterValueChangeCallback(_ =>
            {
                serializedObject.ApplyModifiedProperties();
                resolvedThemeField.value = ResolveThemeForEditor((ThemeBinding)target);
            });

            // Блок биндингов
            root.schedule.Execute(() =>
            {
                var schemaObj = _schemaProp.objectReferenceValue as ThemeSchema;
                if (!schemaObj)
                {
                    AddWithSpace(root, new HelpBox("Assign Schema to edit bindings.", HelpBoxMessageType.Info), 6);
                }
                else
                {
                    var bindingsField = new PropertyField(_bindingsProp, "Bindings");
                    AddWithSpace(root, bindingsField, 6);
                    bindingsField.Bind(serializedObject);

                    var addBtn = new UnityEngine.UIElements.Button(() =>
                        {
                            _bindingsProp.arraySize++;
                            serializedObject.ApplyModifiedProperties();
                            bindingsField.Bind(serializedObject);
                        })
                        { text = "+ Add Binding" };
                    AddWithSpace(root, addBtn, 4);
                }
            }).ExecuteLater(0);

            root.Bind(serializedObject);
            return root;
        }

        private ThemeBase ResolveThemeForEditor(ThemeBinding comp)
        {
            var f = typeof(ThemeConsumer).GetField("currentTheme", BindingFlags.Instance | BindingFlags.NonPublic);
            var cur = f?.GetValue(comp) as ThemeBase;
            if (cur) return cur;

            var schemaProp = serializedObject.FindProperty("schema");
            var schema = schemaProp?.objectReferenceValue as ThemeSchema;
            if (!schema) return null;

            var svc = UnityEngine.Object.FindFirstObjectByType<ThemeService>();
#if UNITY_EDITOR
            if (svc != null)
            {
                foreach (var e in svc.entries)
                    if (e.schema == schema && e.editorTheme)
                        return e.editorTheme;
            }

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

    [CustomPropertyDrawer(typeof(ThemeBinding.Binding))]
    public class ThemeBindingBindingDrawer : PropertyDrawer
    {
        private static readonly HashSet<string> s_Expanded = new HashSet<string>();
        private static readonly Color kValid = new Color(0.20f, 0.80f, 0.30f, 1f);
        private static readonly Color kInvalid = new Color(0.90f, 0.20f, 0.20f, 1f);
        private static readonly Color kBtnOn = new Color(0.18f, 0.48f, 0.95f, 0.25f);

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            root.style.marginTop = 4;
            root.style.marginBottom = 4;
            root.style.paddingLeft = 6;
            root.style.paddingRight = 6;
            root.style.paddingTop = 6;
            root.style.paddingBottom = 6;
            root.style.borderTopLeftRadius = 6;
            root.style.borderTopRightRadius = 6;
            root.style.borderBottomLeftRadius = 6;
            root.style.borderBottomRightRadius = 6;
            root.style.borderBottomWidth = 1;
            root.style.borderTopWidth = 1;
            root.style.borderLeftWidth = 1;
            root.style.borderRightWidth = 1;
            var border = new Color(0, 0, 0, 0.2f);
            root.style.borderBottomColor = border;
            root.style.borderTopColor = border;
            root.style.borderLeftColor = border;
            root.style.borderRightColor = border;
            root.style.flexDirection = FlexDirection.Column;

            static void AddWithSpace(VisualElement parent, VisualElement child, float space = 4)
            {
                if (parent.childCount > 0) child.style.marginTop = space;
                parent.Add(child);
            }

            // Подполя
            var keyProp = property.FindPropertyRelative("key");
            var targetProp = property.FindPropertyRelative("target");
            var graphicProp = property.FindPropertyRelative("graphic");
            var selectableProp = property.FindPropertyRelative("selectable");
#if TMP_PRESENT
            var tmpTextProp = property.FindPropertyRelative("tmpText");
#endif
            var materialProp = property.FindPropertyRelative("material");
            var matPropProp = property.FindPropertyRelative("matProperty");
            var componentProp = property.FindPropertyRelative("component");
            var memberProp = property.FindPropertyRelative("memberName");
            var onColorProp = property.FindPropertyRelative("onColor");
            var onStringProp = property.FindPropertyRelative("onString");

            // Хедер и элементы UI, которые переиспользуются
            var header = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };

            var statusDot = new VisualElement();
            statusDot.style.width = 3;
            statusDot.style.height = 3;
            statusDot.style.marginRight = 6;
            statusDot.style.backgroundColor = kInvalid;
            statusDot.style.borderTopLeftRadius = 3;
            statusDot.style.borderTopRightRadius = 3;
            statusDot.style.borderBottomLeftRadius = 3;
            statusDot.style.borderBottomRightRadius = 3;

            var title = new Label(GetBindingTitle(property))
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, flexGrow = 1 }
            };

            var settingsBtn = new UnityEngine.UIElements.Button() { tooltip = "Settings" };
            settingsBtn.text = "";
            settingsBtn.style.width = 18;
            settingsBtn.style.height = 18;
            settingsBtn.style.borderTopLeftRadius = 4;
            settingsBtn.style.borderTopRightRadius = 4;
            settingsBtn.style.borderBottomLeftRadius = 4;
            settingsBtn.style.borderBottomRightRadius = 4;

            header.Add(statusDot);
            header.Add(title);
            header.Add(settingsBtn);
            AddWithSpace(root, header, 0);

            // Компактная строка и полный блок
            var compactRow = new VisualElement
                { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            var fullBody = new VisualElement { style = { flexDirection = FlexDirection.Column } };
            var fullDynamic = new VisualElement { style = { flexDirection = FlexDirection.Column } };

            AddWithSpace(root, compactRow, 4);
            AddWithSpace(root, fullBody, 4);

            // Key popup (один экземпляр)
            var keyPopup = BuildKeyPopup(property.serializedObject, keyProp);
            keyPopup.style.flexGrow = 1;

            // Target enum (один экземпляр, без дублирования привязки)
            var targetEnum = new EnumField((ThemeBinding.TargetKind)targetProp.enumValueIndex);
            targetEnum.bindingPath = targetProp.propertyPath;
            targetEnum.label = null;
            targetEnum.Bind(property.serializedObject);
            targetEnum.RegisterValueChangedCallback(_ =>
            {
                RebuildDynamicFields();
                UpdateValidity();
            });

            // Компакт: [●] Key | Target | [□]
            void BuildCompactRow()
            {
                compactRow.Clear();
                // Пересобрать шапку под компакт: без title
                header.Clear();
                header.Add(statusDot);
                header.Add(keyPopup);
                var separatorSpace = new VisualElement { style = { width = 6 } };
                header.Add(separatorSpace);
                header.Add(targetEnum);
                header.Add(settingsBtn);

                // Свернуть: скрыть и очистить полный блок, чтобы он не оставался на экране
                fullBody.Clear();
                fullBody.style.display = DisplayStyle.None;
            }

            // Полный: title в шапке, всё остальное вниз
            var topRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            var autoBtn = new UnityEngine.UIElements.Button(() =>
                {
                    AutoDetectTarget(property);
                    property.serializedObject.ApplyModifiedProperties();
                    RebuildDynamicFields();
                    UpdateValidity();
                })
                { text = "Auto-detect" };
            var spacer = new VisualElement { style = { flexGrow = 1 } };
            topRow.Add(spacer);
            topRow.Add(autoBtn);

            // Поле Target с подписью (используем тот же enum, оборачивая в ряд с Label)
            var targetLabeledRow = new VisualElement
                { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            var targetLabel = new Label("Target") { style = { width = 120 } };
            targetEnum.style.marginLeft = 0;
            targetEnum.style.width = StyleKeyword.Auto;
            targetEnum.style.flexGrow = 1;
            targetLabeledRow.Add(targetLabel);
            targetLabeledRow.Add(targetEnum);

            void BuildFullBody()
            {
                // Вернуть шапку в «полный» формат: [●] Binding #N ... [□]
                header.Clear();
                header.Add(statusDot);
                header.Add(title);
                header.Add(settingsBtn);

                fullBody.Clear();
                // Развернуть: показать полный блок
                fullBody.style.display = DisplayStyle.Flex;
                fullBody.Add(topRow);

                keyPopup.style.marginTop = 4;
                fullBody.Add(keyPopup);

                targetLabeledRow.style.marginTop = 4;
                fullBody.Add(targetLabeledRow);

                fullDynamic.style.marginTop = 4;
                fullBody.Add(fullDynamic);

                RebuildDynamicFields();
            }

            // Динамические поля: явные ObjectField/TextField + IMGUIContainer для UnityEvent
            void RebuildDynamicFields()
            {
                fullDynamic.Clear();

                void AddObject<T>(SerializedProperty sp, string label) where T : UnityEngine.Object
                {
                    var f = new ObjectField(label) { objectType = typeof(T), allowSceneObjects = true };
                    f.bindingPath = sp.propertyPath;
                    f.Bind(property.serializedObject);
                    if (fullDynamic.childCount > 0) f.style.marginTop = 2;
                    fullDynamic.Add(f);
                }

                void AddText(SerializedProperty sp, string label)
                {
                    var f = new TextField(label) { isDelayed = true };
                    f.bindingPath = sp.propertyPath;
                    f.Bind(property.serializedObject);
                    if (fullDynamic.childCount > 0) f.style.marginTop = 2;
                    fullDynamic.Add(f);
                }

                void AddUnityEvent(SerializedProperty sp, string label)
                {
                    var box = new IMGUIContainer(() =>
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(sp, new GUIContent(label), includeChildren: true);
                        EditorGUI.indentLevel--;
                    });
                    if (fullDynamic.childCount > 0) box.style.marginTop = 2;
                    fullDynamic.Add(box);
                }

                var tk = (ThemeBinding.TargetKind)targetProp.enumValueIndex;
                switch (tk)
                {
                    case ThemeBinding.TargetKind.Auto:
                        fullDynamic.Add(new HelpBox("Auto: выбирается по первым найденным целям.",
                            HelpBoxMessageType.None));
                        break;

                    case ThemeBinding.TargetKind.GraphicColor:
                        AddObject<Graphic>(graphicProp, "Graphic");
                        break;

                    case ThemeBinding.TargetKind.SelectableColors:
                        AddObject<Selectable>(selectableProp, "Selectable");
                        break;

#if TMP_PRESENT
                    case ThemeBinding.TargetKind.TMPTextColor:
                        AddObject<TMP_Text>(tmpTextProp, "TMP Text");
                        break;
#endif
                    case ThemeBinding.TargetKind.MaterialColorProperty:
                        AddObject<Material>(materialProp, "Material");
                        AddText(matPropProp, "Mat Property");
                        break;

                    case ThemeBinding.TargetKind.FieldOrProperty:
                        AddObject<Component>(componentProp, "Component");
                        AddText(memberProp, "Member Name");
                        break;

                    case ThemeBinding.TargetKind.UnityEventColor:
                        AddUnityEvent(onColorProp, "On Color (Color)");
                        break;

                    case ThemeBinding.TargetKind.UnityEventString:
                        AddUnityEvent(onStringProp, "On Color (String)");
                        break;
                }
            }

            // Переключение режимов и подсветка кнопки
            void RebuildBody()
            {
                var expanded = s_Expanded.Contains(property.propertyPath);
                // Компактный: всё в одну строку (в header), полный: шапка + тело
                if (!expanded) BuildCompactRow();
                else BuildFullBody();

                settingsBtn.style.backgroundColor = expanded ? kBtnOn : StyleKeyword.Null;
                UpdateValidity();
            }

            settingsBtn.clicked += () =>
            {
                var path = property.propertyPath;
                if (!s_Expanded.Remove(path)) s_Expanded.Add(path);
                RebuildBody();
            };

            // Валидация
            bool IsValid()
            {
                var keySchema = keyProp.FindPropertyRelative("schema").objectReferenceValue;
                var keyGuid = keyProp.FindPropertyRelative("keyGuid").stringValue;
                if (keySchema == null || string.IsNullOrEmpty(keyGuid)) return false;

                var tk = (ThemeBinding.TargetKind)targetProp.enumValueIndex;
                return tk switch
                {
                    ThemeBinding.TargetKind.GraphicColor => graphicProp.objectReferenceValue != null,
                    ThemeBinding.TargetKind.SelectableColors => selectableProp.objectReferenceValue != null,
#if TMP_PRESENT
                    ThemeBinding.TargetKind.TMPTextColor          => tmpTextProp.objectReferenceValue    != null,
#endif
                    ThemeBinding.TargetKind.MaterialColorProperty => materialProp.objectReferenceValue != null
                                                                     && !string.IsNullOrEmpty(matPropProp.stringValue),
                    ThemeBinding.TargetKind.FieldOrProperty => componentProp.objectReferenceValue != null
                                                               && !string.IsNullOrEmpty(memberProp.stringValue),
                    _ => true // Auto / UnityEvent* не требуют обязательных ссылок
                };
            }

            void UpdateValidity()
            {
                statusDot.style.backgroundColor = IsValid() ? kValid : kInvalid;
            }

            // Обновления заголовка и валидации
            root.schedule.Execute(() => title.text = GetBindingTitle(property)).Every(200);
            root.RegisterCallback<SerializedPropertyChangeEvent>(_ => UpdateValidity());

            // Гарантируем, что key.schema не пуст
            EnsureKeySchema(property.serializedObject, keyProp);

            // Первая сборка
            RebuildBody();

            return root;

            static string GetBindingTitle(SerializedProperty p)
            {
                var start = p.propertyPath.LastIndexOf('[');
                var end = p.propertyPath.LastIndexOf(']');
                int i = 0;
                if (start >= 0 && end > start)
                    int.TryParse(p.propertyPath.Substring(start + 1, end - start - 1), out i);
                return $"Binding #{i + 1}";
            }
        }

        private static VisualElement BuildKeyPopup(SerializedObject so, SerializedProperty keyProp)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };

            EnsureKeySchema(so, keyProp);
            var schemaSp = keyProp.FindPropertyRelative("schema");
            var guidSp = keyProp.FindPropertyRelative("keyGuid");

            var popup = new PopupField<string>();
            popup.style.flexGrow = 1;

            void Refresh()
            {
                var schema = schemaSp.objectReferenceValue as ThemeSchema;
                if (!schema)
                {
                    popup.choices = Array.Empty<string>().ToList();
                    popup.value = "";
                    return;
                }

                var names = schema.keys.Select(k => k.name).ToList();
                var guids = schema.keys.Select(k => k.guid).ToList();

                popup.choices = names;
                var curIndex = Mathf.Max(0, guids.IndexOf(guidSp.stringValue));
                popup.value = names.Count == 0 ? "" : names[Mathf.Clamp(curIndex, 0, names.Count - 1)];

                popup.RegisterValueChangedCallback(evt =>
                {
                    var idx = names.IndexOf(evt.newValue);
                    if (idx >= 0 && idx < guids.Count)
                    {
                        guidSp.stringValue = guids[idx];
                        so.ApplyModifiedProperties();
                    }
                });
            }

            Refresh();
            row.Add(popup);
            return row;
        }

        private static void EnsureKeySchema(SerializedObject so, SerializedProperty keyProp)
        {
            var schemaSp = keyProp.FindPropertyRelative("schema");
            if (schemaSp.objectReferenceValue == null)
            {
                var comp = so.targetObject as ThemeBinding;
                var schema = comp != null ? comp.ResolveEditorSchema() : null;
                if (schema != null)
                {
                    schemaSp.objectReferenceValue = schema;
                    so.ApplyModifiedProperties();
                }
            }
        }

        private static void AutoDetectTarget(SerializedProperty bindingProp)
        {
            var target = bindingProp.FindPropertyRelative("target");
            var graphic = bindingProp.FindPropertyRelative("graphic");
            var selectable = bindingProp.FindPropertyRelative("selectable");
#if TMP_PRESENT
            var tmpText = bindingProp.FindPropertyRelative("tmpText");
#endif
            var material = bindingProp.FindPropertyRelative("material");
            var component = bindingProp.FindPropertyRelative("component");
            var member = bindingProp.FindPropertyRelative("memberName");

            if (selectable.objectReferenceValue)
                target.enumValueIndex = (int)ThemeBinding.TargetKind.SelectableColors;
            else if (graphic.objectReferenceValue)
                target.enumValueIndex = (int)ThemeBinding.TargetKind.GraphicColor;
#if TMP_PRESENT
            else if (tmpText.objectReferenceValue)
                target.enumValueIndex = (int)ThemeBinding.TargetKind.TMPTextColor;
#endif
            else if (material.objectReferenceValue)
                target.enumValueIndex = (int)ThemeBinding.TargetKind.MaterialColorProperty;
            else if (component.objectReferenceValue && !string.IsNullOrEmpty(member.stringValue))
                target.enumValueIndex = (int)ThemeBinding.TargetKind.FieldOrProperty;
            else
                target.enumValueIndex = (int)ThemeBinding.TargetKind.UnityEventColor;
        }
    }
}
#endif