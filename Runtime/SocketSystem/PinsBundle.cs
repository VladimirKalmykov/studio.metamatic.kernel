using System;
using UnityEngine;

namespace Metamatic
{
    interface IPinsBundle
    {
        public IPin[] GetPins();
    }
    [Serializable]
    public class PinsBundle: IPinsBundle
    {
        public virtual IPin[] GetPins()
        {
            return new IPin[] { };
        }
    }
    
    [Serializable]
    public class PinsBundle<T1>: PinsBundle, IPinsBundle
    {
        [SerializeField]
        public Pin<T1> pin1;
        
        public override IPin[] GetPins()
        {
            return new IPin[] { pin1 };
        }
    }
    
    public class VirtualPinsBundle<T1>: PinsBundle<T1>
    {
        public IPin<T1> virtualPin1;
        
        public override IPin[] GetPins()
        {
            return new IPin[] { virtualPin1 };
        }
    }
}