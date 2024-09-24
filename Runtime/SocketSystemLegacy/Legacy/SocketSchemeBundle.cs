using System;

namespace Metamatic
{
    public abstract class SocketSchemeBundle
    {
        #if DEBUG
        private UnityEngine.Object m_RelatedObject;
        public UnityEngine.Object debug_RelatedObject => m_RelatedObject;
        #endif
        public abstract Type[] GetSocketTypes();

        public bool IsSupportMultipleConnections()
        {
            return false; // TODO: Implement
        }
        
        #if DEBUG
        public void debug_SetRelatedObject(UnityEngine.Object relatedObject)
        {
            m_RelatedObject = relatedObject;
        }
        #endif
    }
    
    [Serializable]
    public class SocketSchemeBundle<TScheme1> : SocketSchemeBundle
    where TScheme1 : SocketScheme, new()
    {
        public override Type[] GetSocketTypes()
        {
            return new Type[]
            {
                typeof(SocketLegacy<TScheme1>)
            };
        }
    }
    
    [Serializable]
    public class SocketSchemeBundle<TScheme1, TScheme2> : SocketSchemeBundle
        where TScheme1 : SocketScheme, new()
        where TScheme2 : SocketScheme, new()
    {
        public override Type[] GetSocketTypes()
        {
            return new Type[]
            {
                typeof(SocketLegacy<TScheme1>),
                typeof(SocketLegacy<TScheme2>)
            };
        }
    }
}