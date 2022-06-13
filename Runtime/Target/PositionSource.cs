using UnityEngine;

namespace Metamatic
{
    public abstract class PositionSource : MonoBehaviour
    {
        
        public Vector3 position
        {
            get => GetPosition();
        }

        protected abstract Vector3 GetPosition();
    }
}