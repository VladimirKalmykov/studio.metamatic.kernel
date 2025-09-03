    namespace Metamatic
{
    using System;
    using System.Collections.Generic;
    using Sirenix.OdinInspector;
    using UnityEngine;

    [CreateAssetMenu(fileName = "ThemeSchema", menuName = "Metamatic/Theme/Schema", order = 1)]
    public class ThemeSchema : ScriptableObject
    {
        [Serializable]
        public class Key
        {
            [ReadOnly] public string guid = System.Guid.NewGuid().ToString();
            [HorizontalGroup("r"), LabelText("Name")] public string name;   // человекочитаемое имя (уникально в рамках схемы)
            [HorizontalGroup("r"), LabelText("Kind")] public ThemeValueKind kind;
        }

        [TableList] public List<Key> keys = new();

        public int FindIndexByGuid(string g) => keys.FindIndex(k => k.guid == g);
        public int FindIndexByName(string n) => keys.FindIndex(k => k.name == n);
    }

}