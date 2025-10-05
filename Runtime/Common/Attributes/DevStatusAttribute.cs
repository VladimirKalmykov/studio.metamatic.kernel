using System;

namespace Metamatic
{
    public enum DevRating
    {
        /// <summary>
        /// Must have. Proved its usefulness.
        /// </summary>
        Core,
        /// <summary>
        /// It works, but not perfect. May be it should be replaced with a better solution.
        /// </summary>
        Ok,
        /// <summary>
        /// It's not clear yet how useful or reliable it is.
        /// </summary>
        Experimental,
        /// <summary>
        /// Still in use, but will be replaced with must suitable alternative
        /// </summary>
        Deprecated
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false)]
    public class DevStatusAttribute: Attribute
    {
        public DevRating rating { get; }

        public DevStatusAttribute(DevRating rating)
        {
            this.rating = rating;
        }
    }
}