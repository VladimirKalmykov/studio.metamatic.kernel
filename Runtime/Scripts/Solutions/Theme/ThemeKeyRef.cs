namespace Metamatic
{
    using System;
    using Sirenix.OdinInspector;
    using UnityEngine;

    [Serializable]
    public struct ThemeKeyRef
    {
        [AssetsOnly] public ThemeSchema schema;

        [ValueDropdown("@schema ? schema.keys : null", IsUniqueList = false, DropdownTitle = "Theme Keys")]
        [SerializeField] private string keyGuid;

        public bool IsValid => schema && !string.IsNullOrEmpty(keyGuid);

        public ThemeSchema.Key Resolve()
        {
            var selfKeyGuid = keyGuid;
            return !IsValid ? null : schema.keys.Find(k => k.guid == selfKeyGuid);
        }
        public ThemeValueKind Kind => Resolve()?.kind ?? default;
        public int Index => schema ? schema.FindIndexByGuid(keyGuid) : -1;

        // ✔ helper для безопасного присваивания схемы
        public ThemeKeyRef WithSchema(ThemeSchema s)
        {
            var k = this;
            if (!k.schema) k.schema = s;
            return k;
        }
    }
}