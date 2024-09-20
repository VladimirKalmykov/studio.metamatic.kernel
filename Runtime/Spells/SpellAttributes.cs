namespace Spells
{
    public class SpellAttribute: System.Attribute
    {
        public string Name;
        public string Description;
        public string IconPath;
        public SpellAttribute(string name, string description, string iconPath = null)
        {
            Name = name;
            Description = description;
            IconPath = iconPath;
        }
    }
}