// ThemeConsumerBase.cs
// Один consumer — одна схема. Работает с вашим ThemeServiceV2.

namespace Metamatic
{
    using System;
    using Cysharp.Threading.Tasks;
    using Metamatic;
    using Sirenix.OdinInspector;
    using UnityEngine;

    [ExecuteAlways]
    [DisallowMultipleComponent]
    public abstract class ThemeConsumer : MonoBehaviour
    {
        [Header("Theme")]
        [SerializeField] private ThemeSchema schema;
        public ThemeSchema Schema => schema;

        [ShowInInspector, ReadOnly] protected ThemeBase currentTheme;

        private SignalSource _disable;

#if UNITY_EDITOR
        private DisposableAction _editorThemeWatch;
        private ThemeBase _editorLastTheme;
#endif

        protected virtual void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorResolve();
                EditorSubscribeToThemeChange();
                SafeApply();
                return;
            }
#endif
            if (!schema)
            {
                Debug.LogWarning($"{nameof(ThemeConsumer)} on {name}: Schema is not assigned.", this);
                return;
            }

            _disable = SignalSource.Create();
            EnableAsync(_disable.Signal).Forget();
        }

        protected virtual void OnDisable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                _editorThemeWatch.Dispose();
                _editorThemeWatch = default;
                _editorLastTheme = null;
                return;
            }
#endif
            _disable.TryRaise();
        }

        private async UniTaskVoid EnableAsync(Signal signal)
        {
            try
            {
                var svc = await Service.Resolve<ThemeService>(signal);
                using var watchTheme = svc.WatchTheme(schema, OnThemeChanged);
                await signal;
            }
            catch (OperationCanceledException) { }
            catch (Exception e) { Debug.LogException(e, this); }
        }

        private void OnThemeChanged(ThemeBase t)
        {
            if (!t) return;
            currentTheme = t;
            SafeApply();
        }

        protected void SafeApply()
        {
            if (!currentTheme || !schema) return;
            try { ApplyColors(currentTheme); }
            catch (Exception e) { Debug.LogException(e, this); }
#if UNITY_EDITOR
            if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

#if UNITY_EDITOR
        protected virtual void Update()
        {
            if (!Application.isPlaying)
            {
                var before = currentTheme;
                EditorResolve();
                if (currentTheme != before) EditorSubscribeToThemeChange();
                if (IsSelectedInEditor())
                {
                    SafeApply();
                }
            }
        }

        protected virtual void OnValidate()
        {
            if (!Application.isPlaying)
            {
                EditorResolve();
                EditorSubscribeToThemeChange();
                SafeApply();
            }
        }

        private void EditorResolve()
        {
            if (!schema) { currentTheme = null; return; }

            // 1) Если в сцене есть ThemeServiceV2 — попробуем из его entries взять editorTheme по нашей схеме
            var svc = UnityEngine.Object.FindFirstObjectByType<ThemeService>();
            if (svc != null)
            {
#if UNITY_EDITOR
                foreach (var entry in svc.entries)
                {
                    if (entry.schema == schema && entry.editorTheme)
                    {
                        currentTheme = entry.editorTheme;
                        return;
                    }
                }
#endif
            }

            // 2) Найдём любой Theme ассет с нужной схемой
#if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets("t:Theme");
            foreach (var g in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                var theme = UnityEditor.AssetDatabase.LoadAssetAtPath<Theme>(path);
                if (theme && theme.schema == schema) { currentTheme = theme; return; }
            }
#endif
            // Ничего не нашли — оставим как есть.
        }

        private void EditorSubscribeToThemeChange()
        {
            if (Application.isPlaying) return;
            if (currentTheme == _editorLastTheme) return;

            _editorThemeWatch.Dispose();
            _editorThemeWatch = default;
            _editorLastTheme = currentTheme;
            if (currentTheme)
                _editorThemeWatch = currentTheme.Watch(SafeApply);
        }

        private bool IsSelectedInEditor()
        {
            var go = gameObject;
            if (!go) return false;
            return UnityEditor.Selection.Contains(go);
        }
#endif

        protected abstract void ApplyColors(ThemeBase theme);
    }
}
