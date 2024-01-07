using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metamatic
{
    public interface IPlug
    {
        
    }

    public interface IPlug<TScheme>: IPlug
    where TScheme: SocketScheme
    {
        public TScheme scheme { get; }
    }
    
    public abstract class Plug: MonoBehaviour, IPlug
    {
        public Pin[] outputPins;
        public Pin[] inputPins;
        public Type[] inputTypes => GetInputTypes();
        public Type[] outputTypes => GetOutputTypes();
        public Type[] inputPinTypes => GetInputPinTypes();
        public Type[] outputPinTypes => GetOutputPinTypes();
        protected abstract Type[] GetInputTypes();
        protected abstract Type[] GetOutputTypes();
        protected abstract Type[] GetInputPinTypes();
        protected abstract Type[] GetOutputPinTypes();
        public abstract SocketScheme socketScheme { get; }
    }
    
    public abstract class Plug<TScheme>: Plug, IPlug<TScheme>, ISerializationCallbackReceiver
    where TScheme: SocketScheme, new()
    {
        [ShowInInspector, ReadOnly]
        private Socket<TScheme> m_Socket;
        private TScheme m_Scheme;
        public TScheme scheme => m_Scheme ??= new TScheme();
        public override SocketScheme socketScheme => scheme;
        
        protected virtual void OnEnable()
        {
            scheme.debug_SetRelatedObject(this);
            /*
             * Initialize the scheme with the input and output pins, but switch the input and output pins because plug has reversed input and output pins.
             */
            scheme.Initialize(inputPins.Cast<IPin>().ToArray(), outputPins.Cast<IPin>().ToArray());
        }

        protected virtual void OnDisable()
        {
            scheme.Dispose();
        }
        
        public void OnBeforeSerialize()
        {
            Array.Resize(ref inputPins, scheme.outputTypes.Length);
            Array.Resize(ref outputPins, scheme.inputTypes.Length);
        }

        public void OnAfterDeserialize()
        {
            Array.Resize(ref inputPins, scheme.outputTypes.Length);
            Array.Resize(ref outputPins, scheme.inputTypes.Length);
        }
        
        protected override Type[] GetInputTypes()
        {
            return scheme.outputTypes;
        }

        protected override Type[] GetOutputTypes()
        {
            return scheme.inputTypes;
        }
        protected override Type[] GetInputPinTypes()
        {
            return scheme.outputPinTypes;
        }
        protected override Type[] GetOutputPinTypes()
        {
            return scheme.inputPinTypes;
        }
    }
}