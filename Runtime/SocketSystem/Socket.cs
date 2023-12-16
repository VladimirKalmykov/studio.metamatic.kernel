using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metamatic
{
    public abstract class Socket: MonoBehaviour
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
    
    public class Socket<TScheme>: Socket, ISerializationCallbackReceiver
    where TScheme: SocketScheme, new()
    {
        
        public bool loopback;
        
        [ShowInInspector, ReadOnly]
        private List<Plug<TScheme>> plugs;
        private TScheme m_Scheme;
        public TScheme scheme => m_Scheme ??= new TScheme();
        public override SocketScheme socketScheme => scheme;

        private void OnEnable()
        {
            scheme.debug_SetRelatedObject(this);
            scheme.Initialize(outputPins, inputPins, loopback);
        }

        private void OnDisable()
        {
            scheme.Dispose();
        }

        public Action ConnectPlug(Plug<TScheme> plug)
        {
            return () => { };
        }

        public void OnBeforeSerialize()
        {
            Array.Resize(ref inputPins, scheme.inputTypes.Length);
            Array.Resize(ref outputPins, scheme.outputTypes.Length);
        }

        public void OnAfterDeserialize()
        {
            Array.Resize(ref inputPins, scheme.inputTypes.Length);
            Array.Resize(ref outputPins, scheme.outputTypes.Length);
        }

        protected override Type[] GetInputTypes()
        {
            return scheme.inputTypes;
        }

        protected override Type[] GetOutputTypes()
        {
            return scheme.outputTypes;
        }
        protected override Type[] GetInputPinTypes()
        {
            return scheme.inputPinTypes;
        }
        protected override Type[] GetOutputPinTypes()
        {
            return scheme.outputPinTypes;
        }
    }
}