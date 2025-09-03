using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif

namespace Metamatic
{
    public class ThemeBinding : ThemeConsumer
    {
        public enum TargetKind
        {
            Auto,
            GraphicColor,
            SelectableColors,
            FieldOrProperty,
            TMPTextColor,
            MaterialColorProperty,
            UnityEventColor,
            UnityEventString
        }

        [Header("Editor Only")]
        [Tooltip("Подсказка редактору: если currentTheme отсутствует, ключи будем выбирать из этой схемы")]
        [SerializeField] private ThemeSchema editorSchemaHint;

        [Serializable]
        public class Binding
        {
            public ThemeKeyRef key;
            public TargetKind target = TargetKind.Auto;

            // цели (показываются в кастомном инспекторе по target)
            public Graphic   graphic;
            public Selectable selectable;
#if TMP_PRESENT
            public TMP_Text  tmpText;
#endif
            public Material  material;
            public string    matProperty = "_Color";
            public Component component;
            public string    memberName;
            public UnityEvent<Color>   onColor;
            public UnityEvent<string>  onString;

            // кеш сеттеров
            [NonSerialized] public Action<Color> setColor;
            [NonSerialized] public Action<ColorBlock> setBlock;
            [NonSerialized] public bool initialized;
        }

        [SerializeField] private List<Binding> bindings = new();

        // Даст схему для инспектора и автопроставления в ключах
        internal ThemeSchema ResolveEditorSchema()
        {
            return currentTheme ? currentTheme.schema : editorSchemaHint;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            var schema = ResolveEditorSchema();
            if (!schema) return;

            for (int i = 0; i < bindings.Count; i++)
            {
                var k = bindings[i].key;
                if (!k.schema)               // ← struct, проверяем поле
                {
                    k = k.WithSchema(schema); // ← вернёт копию с проставленной схемой
                    bindings[i].key = k;
                }
                // Если k.schema уже задана и это другая схема — оставляем как есть
                // (либо тут можешь вывести Warning, если тебе нельзя смешивать схемы).
            }
        }
#endif

        protected override void ApplyColors(ThemeBase theme)
        {
            foreach (var b in bindings)
            {
                if (!b.key.IsValid) continue;
                Init(b);

                switch (b.key.Kind)
                {
                    case ThemeValueKind.Color:
                        if (theme.TryGetColor(b.key, out var c)) Apply(b, c);
                        break;
                    case ThemeValueKind.ColorBlock:
                        if (theme.TryGetColorBlock(b.key, out var cb)) Apply(b, cb);
                        break;
                }
            }
        }

        private void Init(Binding b)
        {
            if (b.initialized) return;

            if (b.target == TargetKind.Auto)
            {
                if (b.selectable) b.target = TargetKind.SelectableColors;
                else if (b.graphic) b.target = TargetKind.GraphicColor;
#if TMP_PRESENT
                else if (b.tmpText) b.target = TargetKind.TMPTextColor;
#endif
                else if (b.material) b.target = TargetKind.MaterialColorProperty;
                else if (b.component && !string.IsNullOrEmpty(b.memberName)) b.target = TargetKind.FieldOrProperty;
                else b.target = TargetKind.UnityEventColor;
            }

            switch (b.target)
            {
                case TargetKind.GraphicColor:
                    b.setColor = c => { if (b.graphic) { b.graphic.color = c; MarkDirty(b.graphic); } };
                    break;

                case TargetKind.TMPTextColor:
#if TMP_PRESENT
                    b.setColor = c => { if (b.tmpText) { b.tmpText.color = c; MarkDirty(b.tmpText); } };
#else
                    b.setColor = null;
#endif
                    break;

                case TargetKind.SelectableColors:
                    b.setBlock = cb => { if (b.selectable) { b.selectable.colors = cb; MarkDirty(b.selectable); } };
                    b.setColor = c => {
                        if (!b.selectable) return;
                        var cb = b.selectable.colors; cb.normalColor = c; b.selectable.colors = cb; MarkDirty(b.selectable);
                    };
                    break;

                case TargetKind.MaterialColorProperty:
                    b.setColor = c => {
                        if (b.material && b.material.HasProperty(b.matProperty))
                            b.material.SetColor(b.matProperty, c);
                    };
                    break;

                case TargetKind.FieldOrProperty:
                    if (b.component && !string.IsNullOrEmpty(b.memberName))
                    {
                        var setterC = CompileSetter<Color>(b.component, b.memberName);
                        var setterB = CompileSetter<ColorBlock>(b.component, b.memberName);
                        b.setColor = c => { setterC?.Invoke(c); MarkDirty(b.component); };
                        b.setBlock = cb => { setterB?.Invoke(cb); MarkDirty(b.component); };
                    }
                    break;

                case TargetKind.UnityEventColor:
                    b.setColor = c => b.onColor?.Invoke(c);
                    break;

                case TargetKind.UnityEventString:
                    b.setColor = c => b.onString?.Invoke("#" + ColorUtility.ToHtmlStringRGBA(c));
                    break;
            }

            b.initialized = true;
        }

        private static Action<T> CompileSetter<T>(Component comp, string member)
        {
            var t = comp.GetType();
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var f = t.GetField(member, flags);
            if (f != null && f.FieldType == typeof(T))
            {
                var inst = Expression.Constant(comp);
                var val = Expression.Parameter(typeof(T), "v");
                var fe = Expression.Field(Expression.Convert(inst, t), f);
                var assign = Expression.Assign(fe, val);
                return Expression.Lambda<Action<T>>(assign, val).Compile();
            }
            var p = t.GetProperty(member, flags);
            if (p != null && p.PropertyType == typeof(T) && p.CanWrite)
            {
                var inst = Expression.Constant(comp);
                var val = Expression.Parameter(typeof(T), "v");
                var pe = Expression.Property(Expression.Convert(inst, t), p);
                var assign = Expression.Assign(pe, val);
                return Expression.Lambda<Action<T>>(assign, val).Compile();
            }
            return null;
        }

        private static void Apply(Binding b, in Color c) => b.setColor?.Invoke(c);
        private static void Apply(Binding b, in ColorBlock cb)
        {
            if (b.setBlock != null) b.setBlock(cb);
            else b.setColor?.Invoke(cb.normalColor);
        }

        private static void MarkDirty(UnityEngine.Object o)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && o) UnityEditor.EditorUtility.SetDirty(o);
#endif
        }
    }
}
