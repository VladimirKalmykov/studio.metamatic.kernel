using System;

namespace Metamatic
{
    public class ServiceGuideAttribute: Attribute
    {
        public readonly string Guide;
        
        public ServiceGuideAttribute(string guide)
        {
            this.Guide = guide;
        }
    }
}