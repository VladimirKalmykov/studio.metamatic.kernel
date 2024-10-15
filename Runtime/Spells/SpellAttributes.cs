using System;

namespace Metamatic
{
    public class SpellAttribute: System.Attribute
    {
        public string Name;
        public string Description;
        public string IconPath;
        public string Category;
        public SpellAttribute(string name, string description, string iconPath = null, string category = "Common")
        {
            Name = name;
            Description = description;
            IconPath = iconPath;
            Category = category;
        }
        
        public static SpellMetadata GetMetadata(System.Type spellType)
        {
            var spellAttribute = (SpellAttribute)System.Attribute.GetCustomAttribute(spellType, typeof(SpellAttribute));
            if (spellAttribute == null)
            {
                global::Metamatic.Console.Exception("Spell", $"Spell {spellType.Name} does not have SpellAttribute");
                
                return new SpellMetadata
                {
                    SpellType = spellType,
                    Name = spellType.Name,
                    Description = "",
                    IconPath = null,
                    Category = "Common",
                };
            }
            return new SpellMetadata
            {
                SpellType = spellType,
                Name = spellAttribute.Name,
                Description = spellAttribute.Description,
                IconPath = spellAttribute.IconPath,
                Category = spellAttribute.Category,
            };
        }
        
        public static SpellMetadata GetMetadata<T>() where T: ISpell
        {
            return GetMetadata(typeof(T));
        }
    }
}