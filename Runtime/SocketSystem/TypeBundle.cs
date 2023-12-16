using System;

namespace Metamatic
{
    interface ITypeBundle
    {
        public Type[] GetTypes();
    }
    public class TypeBundle: ITypeBundle
    {
        public virtual Type[] GetTypes()
        {
            return new Type[] { };
        }
        
        public virtual Type[] GetPinTypes()
        {
            return new Type[] { };
        }
    }
    
    public class TypeBundle<T1>: TypeBundle, ITypeBundle
    {
        public override Type[] GetTypes()
        {
            return new Type[] { typeof(T1) };
        }
        
        public override Type[] GetPinTypes()
        {
            return new Type[] { typeof(Pin<T1>) };
        }
    }
    
    public class TypeBundle<T1, T2>: TypeBundle, ITypeBundle
    {
        public override Type[] GetTypes()
        {
            return new Type[] { typeof(T1), typeof(T2) };
        }
        
        public override Type[] GetPinTypes()
        {
            return new Type[] { typeof(Pin<T1>), typeof(Pin<T2>) };
        }
    }
    
    public class TypeBundle<T1, T2, T3>: TypeBundle, ITypeBundle
    {
        public override Type[] GetTypes()
        {
            return new Type[] { typeof(T1), typeof(T2), typeof(T3) };
        }
        
        public override Type[] GetPinTypes()
        {
            return new Type[] { typeof(Pin<T1>), typeof(Pin<T2>), typeof(Pin<T3>) };
        }
    }
}