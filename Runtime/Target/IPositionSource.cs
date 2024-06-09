using UnityEngine;

namespace Metamatic
{
    public interface IPositionSource
    {
        public Vector3 position { get; }
        public Vector3 forward { get; }
        public Vector3 right { get; }
        public Vector3 up { get; }
    }
}