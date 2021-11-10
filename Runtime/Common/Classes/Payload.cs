using System;

namespace Metamatic
{
    public class Payload
    {
        public readonly Type type;

        public Payload(Type type)
        {
            this.type = type;
        }
    }

    public class Payload<T> : Payload
    where T : notnull
    {
        public readonly T data;

        public Payload(in T data = default): base(typeof(T))
        {
            this.data = data;
        }
    }
}