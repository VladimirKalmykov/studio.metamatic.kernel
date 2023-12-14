using System;
using UnityEngine;

namespace Metamatic
{
    [Serializable]
    public class SocketScheme
    {
        
    }
    
    [Serializable]
    public class SocketScheme<TOutputBundle, TInputBundle>: SocketScheme
    where TOutputBundle: PinsBundle
    where TInputBundle: PinsBundle
    {
        [SerializeField]
        public TOutputBundle outputBundle;
        [SerializeField]
        public TInputBundle inputBundle;
    }
    
    /// <summary>
    /// Output-only socket
    /// </summary>
    /// <typeparam name="TOutputBundle"></typeparam>
    [Serializable]
    public class SocketScheme<TOutputBundle>: SocketScheme<TOutputBundle, PinsBundle>
    where TOutputBundle: PinsBundle
    {
        
    }
    
    /// <summary>
    /// The input and output pins are the same
    /// </summary>
    /// <typeparam name="TBundle"></typeparam>
    [Serializable]
    public class LoopbackSocketScheme<TBundle>: SocketScheme<TBundle, TBundle>
    where TBundle: PinsBundle
    {
        
    }
    
    [Serializable]
    public class FloatSocketScheme: SocketScheme<PinsBundle<IntPin, FloatPin>, PinsBundle<IntPin>>
    {
        
    }
}