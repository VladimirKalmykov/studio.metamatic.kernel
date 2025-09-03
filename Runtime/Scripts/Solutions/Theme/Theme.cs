// Theme.cs
// -------------------------------------------------

using UnityEngine.UI;

namespace Metamatic
{
    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(fileName = "Theme", menuName = "Metamatic/Theme/Theme", order = 2)]
    public class Theme : ThemeBase
    {
        // Слоты выровнены по индексам schema.keys (сопоставление по GUID)
        [SerializeField, HideInInspector] private List<string> _slotGuids = new();
        [SerializeField, HideInInspector] private List<Color> _colors = new();
        [SerializeField, HideInInspector] private List<ColorBlock> _blocks = new();

        public override bool TryGetColor(in ThemeKeyRef key, out Color color)
        {
            color = default;
            if (!IsKeyForThisSchema(key) || key.Kind != ThemeValueKind.Color) return false;
            EnsureSyncedToSchema();
            var idx = key.Index;
            if (idx < 0 || idx >= _colors.Count) return false;
            color = _colors[idx];
            return true;
        }

        public override bool TryGetColorBlock(in ThemeKeyRef key, out ColorBlock block)
        {
            block = default;
            if (!IsKeyForThisSchema(key) || key.Kind != ThemeValueKind.ColorBlock) return false;
            EnsureSyncedToSchema();
            var idx = key.Index;
            if (idx < 0 || idx >= _blocks.Count) return false;
            block = _blocks[idx];
            return true;
        }

        public bool IsKeyForThisSchema(in ThemeKeyRef key)
        {
            return key.IsValid && schema && key.schema == schema;
        }

        public void EnsureSyncedToSchema()
        {
            if (!schema)
            {
                _slotGuids.Clear();
                _colors.Clear();
                _blocks.Clear();
                return;
            }
            if (_slotGuids.Count != schema.keys.Count)
                SyncToSchemaInternal();
        }

        public void ForceSync() => SyncToSchemaInternal();

        private void SyncToSchemaInternal()
        {
            var keys = schema ? schema.keys : null;
            var newCount = keys != null ? keys.Count : 0;

            var guidToOld = new Dictionary<string, int>(_slotGuids.Count);
            for (int i = 0; i < _slotGuids.Count; i++)
            {
                var g = _slotGuids[i];
                if (!string.IsNullOrEmpty(g) && !guidToOld.ContainsKey(g))
                    guidToOld[g] = i;
            }

            var newGuids  = new List<string>(newCount);
            var newColors = new List<Color>(newCount);
            var newBlocks = new List<ColorBlock>(newCount);

            for (int i = 0; i < newCount; i++)
            {
                var guid = keys[i].guid;
                newGuids.Add(guid);

                if (guidToOld.TryGetValue(guid, out var oldIndex))
                {
                    newColors.Add(oldIndex < _colors.Count ? _colors[oldIndex] : default);
                    newBlocks.Add(oldIndex < _blocks.Count ? _blocks[oldIndex] : DefaultBlock());
                }
                else
                {
                    newColors.Add(Color.white);
                    newBlocks.Add(DefaultBlock());
                }
            }

            _slotGuids = newGuids;
            _colors    = newColors;
            _blocks    = newBlocks;

    #if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.EditorUtility.SetDirty(this);
    #endif
        }

        private static ColorBlock DefaultBlock()
        {
            return ColorBlock.defaultColorBlock;
        }

    #if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (!Application.isPlaying) EnsureSyncedToSchema();
        }
    #endif
    }
}
