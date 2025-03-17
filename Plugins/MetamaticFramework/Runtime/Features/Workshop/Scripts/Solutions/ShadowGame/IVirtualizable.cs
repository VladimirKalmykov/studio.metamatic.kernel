namespace Metamatic.Framework
{
    public interface IVirtualizable<T> where T : VirtualBehaviour
    {
        /// <summary>
        /// Reference to the virtual representation of the real object.
        /// </summary>
        public T virtualRepresentation { get; set; }
    }
}