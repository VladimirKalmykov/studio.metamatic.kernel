using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metamatic
{
    public abstract class Socket: MonoBehaviour
    {
        [SerializeField]
        public bool loopback;
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
        public abstract Action Connect(IPlug plug);
        public abstract void Disconnect(IPlug plug);
        #if DEBUG
        public event Action debug_OnValidate;
        private void OnValidate()
        {
            debug_OnValidate?.Invoke();
        }
#endif
    }
    
    public class Socket<TScheme>: Socket, ISerializationCallbackReceiver
    where TScheme: SocketScheme, new()
    {
#if DEBUG
        [ShowInInspector, ReadOnly]
        public UnityEngine.Object[] debugConnectedObjects => scheme.debug_GetConnectedOutSchemes()
            .Select(sch => sch.debug_RelatedObject).ToArray();
#endif
        private TScheme m_Scheme;
        public TScheme scheme => m_Scheme ??= new TScheme();
        public override SocketScheme socketScheme => scheme;

        private void OnEnable()
        {
            scheme.debug_SetRelatedObject(this);
            scheme.Initialize(outputPins.Cast<IPin>().ToArray(), inputPins.Cast<IPin>().ToArray(), loopback);
        }

        private void OnDisable()
        {
            scheme.Dispose();
        }

        public Action Connect(IPlug<TScheme> plug)
        {
            return scheme.ConnectPlug(plug.scheme);
        }

        public override Action Connect(IPlug plug)
        {
            var plugAsGeneric = plug as IPlug<TScheme>;
            if (plugAsGeneric == null)
            {
                throw new Exception($"Plug {plug} is not of type {typeof(TScheme)}");
            }
            return scheme.ConnectPlug(plugAsGeneric.scheme);
        }
        
        public void Disconnect(IPlug<TScheme> plug)
        {
            scheme.DisconnectPlug(plug.scheme);
        }
        
        public override void Disconnect(IPlug plug)
        {
            var plugAsGeneric = plug as IPlug<TScheme>;
            if (plugAsGeneric == null)
            {
                throw new Exception($"Plug {plug} is not of type {typeof(TScheme)}");
            }
            scheme.DisconnectPlug(plugAsGeneric.scheme);
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