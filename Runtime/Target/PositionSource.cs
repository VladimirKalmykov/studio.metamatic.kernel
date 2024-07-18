using UnityEngine;

namespace Metamatic
{
    public abstract class PositionSource : MonoBehaviour, IPositionSource
    {
        
        public Vector3 position
        {
            get => GetPosition();
        }

        public Vector3 forward { get; }
        public Vector3 right { get; }
        public Vector3 up { get; }

        protected abstract Vector3 GetPosition();
    }
}