using UnityEngine;

namespace Metamatic
{
    public class SpatialEventLegacy: InstantEventLegacy
    {
        public Vector3 position;
        public void Emit(in Vector3 position)
        {
            this.position = position;
            base.Emit();
        }
    }
    
    public class SpatialEventLegacy<T> : SpatialEventLegacy
    {
        public T value;

        public void Emit(in Vector3 position, in T value)
        {
            base.Emit(position);
            this.value = value;
        }
    }

}