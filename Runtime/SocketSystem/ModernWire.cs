using System;
using Metamatic;

namespace Metamatic
{
    public class ModernWire: IModernWire
    {
        private readonly Guid m_ID;
        public ModernWire(IModernWire.ConnectionType connectionType = IModernWire.ConnectionType.Multiple)
        {
            m_ID = Guid.NewGuid();
            this.connectionType = connectionType;
        }
        public override int GetHashCode()
        {
            return m_ID.GetHashCode();
        }

        public IModernWire.ConnectionType connectionType { get; private set; }

        public bool Equals(IModernWire anotherWire)
        {
            throw new System.NotImplementedException();
        }
    }
}