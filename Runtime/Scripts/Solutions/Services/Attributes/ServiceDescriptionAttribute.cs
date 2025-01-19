using System;

namespace Metamatic
{
    public class ServiceDescriptionAttribute: Attribute
    {
        public readonly string Description;
        
        public ServiceDescriptionAttribute(string description)
        {
            this.Description = description;
        }
    }
}