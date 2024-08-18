using System;

namespace Metamatic
{
    public interface IAbortable
    {
        bool isAborted { get; }
        event Action OnAborted;

        public void Abort();
    }
}