using System;
using UnityEngine;

namespace Metamatic
{
    [Serializable]
    public class PinsBundle
    {
        public virtual Pin[] GetPins()
        {
            return new Pin[] { };
        }
    }
    
    [Serializable]
    public class PinsBundle<TPin1>: PinsBundle
    where TPin1: Pin
    {
        [SerializeField]
        public TPin1 pin1;
        
        public override Pin[] GetPins()
        {
            return new Pin[] { pin1 };
        }
    }
    
    [Serializable]
    public class PinsBundle<TPin1, TPin2>: PinsBundle
    where TPin1: Pin
    where TPin2: Pin
    {
        [SerializeField]
        public TPin1 pin1;
        [SerializeField]
        public TPin2 pin2;
        
        public override Pin[] GetPins()
        {
            return new Pin[] { pin1, pin2 };
        }
    }
    
    [Serializable]
    public class PinsBundle<TPin1, TPin2, TPin3>: PinsBundle
    where TPin1: Pin
    where TPin2: Pin
    where TPin3: Pin
    {
        [SerializeField]
        public TPin1 pin1;
        [SerializeField]
        public TPin2 pin2;
        [SerializeField]
        public TPin3 pin3;
        
        public override Pin[] GetPins()
        {
            return new Pin[] { pin1, pin2, pin3 };
        }
    }
    
    [Serializable]
    public class PinsBundle<TPin1, TPin2, TPin3, TPin4>: PinsBundle
    where TPin1: Pin
    where TPin2: Pin
    where TPin3: Pin
    where TPin4: Pin
    {
        [SerializeField]
        public TPin1 pin1;
        [SerializeField]
        public TPin2 pin2;
        [SerializeField]
        public TPin3 pin3;
        [SerializeField]
        public TPin4 pin4;
        
        public override Pin[] GetPins()
        {
            return new Pin[] { pin1, pin2, pin3, pin4 };
        }
    }
    
    [Serializable]
    public class PinsBundle<TPin1, TPin2, TPin3, TPin4, TPin5>: PinsBundle
    where TPin1: Pin
    where TPin2: Pin
    where TPin3: Pin
    where TPin4: Pin
    where TPin5: Pin
    {
        [SerializeField]
        public TPin1 pin1;
        [SerializeField]
        public TPin2 pin2;
        [SerializeField]
        public TPin3 pin3;
        [SerializeField]
        public TPin4 pin4;
        [SerializeField]
        public TPin5 pin5;
        
        public override Pin[] GetPins()
        {
            return new Pin[] { pin1, pin2, pin3, pin4, pin5 };
        }
    }
}