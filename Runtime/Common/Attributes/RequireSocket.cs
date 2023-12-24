using System;

namespace Metamatic
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class RequireSocket: Attribute
    {
        public readonly bool optional;
        public Type[] types;
        /// <summary>
        /// Mark that this form-factor requires one or several sockets
        /// </summary>
        /// <param name="type"></param>
        /// <param name="optional"></param>
        /// <exception cref="Exception"></exception>
        public RequireSocket(Type type, bool optional = false)
        {
            if (!type.IsSubclassOf(typeof(SocketScheme)))
            {
                throw new Exception("RequireSocket attribute can be used only for Socket subclasses");
            }
            this.optional = optional;
            this.types = new Type[] { type };
        }
        
        public RequireSocket(Type[] types, bool optional = false)
        {
            foreach (var type in types)
            {
                if (!type.IsSubclassOf(typeof(SocketScheme)))
                {
                    throw new Exception("RequireSocket attribute can be used only for Socket subclasses");
                }
            }
            this.optional = optional;
            this.types = types;
        }
    }
}