namespace Metamatic
{
#if UNITY_EDITOR
    using UnityEngine;


    public class ThemeKindAttribute : PropertyAttribute
    {
        public readonly ThemeValueKind? kind;
        public ThemeKindAttribute() { }
        public ThemeKindAttribute(ThemeValueKind kind) { this.kind = kind; }
    }
#endif
}