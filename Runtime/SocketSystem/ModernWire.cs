using System;
using Metamatic;

namespace Metamatic
{
    public abstract class ModernWire: IModernWire
    {
        private readonly Guid m_ID;
        public event Action OnDispose;
        public ModernWire(IModernWire.ConnectionType connectionType = IModernWire.ConnectionType.Multiple)
        {
            m_ID = Guid.NewGuid();
            this.connectionType = connectionType;
        }
        public override int GetHashCode()
        {
            return m_ID.GetHashCode();
        }

        public virtual void Dispose()
        {
            OnDispose?.Invoke();
        }

        public IModernWire.ConnectionType connectionType { get; private set; }

        public bool Equals(IModernWire anotherWire)
        {
            return GetHashCode() == anotherWire.GetHashCode();
        }
    }
}