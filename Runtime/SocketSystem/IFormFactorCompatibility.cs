using System;
using Metamatic;

namespace Metamatic.SocketSystem
{
    public interface IFormFactorCompatibility
    {
        public Type formFactor { get; }
    }
}