using UnityEngine;

namespace Metamatic
{
    public class SpatialEvent: InstantEvent
    {
        public Vector3 position;
        public void Emit(in Vector3 position)
        {
            this.position = position;
            base.Emit();
        }
    }
    
    public class SpatialEvent<T> : SpatialEvent
    {
        public T value;

        public void Emit(in Vector3 position, in T value)
        {
            base.Emit(position);
            this.value = value;
        }
    }

}