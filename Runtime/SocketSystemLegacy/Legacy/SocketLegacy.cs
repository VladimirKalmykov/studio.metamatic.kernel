using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metamatic
{
    public interface ISocket
    {
        public string socketName { get; }
    }

    public interface ISocket<out TScheme>: ISocket
        where TScheme: SocketScheme
    {
        public TScheme scheme { get; }
    }
    public abstract class SocketLegacy: MonoBehaviour, ISocket
    {
        [SerializeField, LabelText("Name")] protected string p_SocketName;
        [SerializeField, Tooltip("Socket be able to auto connect by global pattern")] protected bool isGlobal;
        [SerializeField, Tooltip("Once auto connected plug cannot disconnect according hierarchy or global changes"), LabelText("Autolock")] protected bool p_AutoLock;
        [SerializeField, HideInInspector]
        public bool loopback;
        [HideInInspector]
        public Pin[] outputPins = Array.Empty<Pin>();
        [HideInInspector]
        public Pin[] inputPins = Array.Empty<Pin>();
        public Type[] inputTypes => GetInputTypes();
        public Type[] outputTypes => GetOutputTypes();
        public Type[] inputPinTypes => GetInputPinTypes();
        public Type[] outputPinTypes => GetOutputPinTypes();
        public string socketName => p_SocketName;
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
    
    public class SocketLegacy<TScheme>: SocketLegacy, ISerializationCallbackReceiver, ISocket<TScheme>
    where TScheme: SocketScheme, new()
    {
#if DEBUG
        public UnityEngine.Object[] debugConnectedObjects => scheme.debug_GetConnectedOutSchemes()
            .Select(sch => sch.debug_RelatedObject).ToArray();
        [HideInInspector]
        public string debugNotice = "";
#endif
        #region Static
        /// <summary>
        /// Contains all sockets in the scene that marked as global.
        /// </summary>
        public static readonly List<SocketLegacy<TScheme>> Registry = new ();
        /// <summary>
        /// Get global socket by name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static SocketLegacy<TScheme> GetByName(string name)
        {
            return Registry.FirstOrDefault(s => s.p_SocketName == name);
        }

        public static event Action OnSocketRegistryChange;
        #endregion
        private TScheme m_Scheme;
        public TScheme scheme
        {
            get
            {
                if (m_Scheme == null)
                {
                    m_Scheme = new TScheme();
                    m_Scheme.debug_SetRelatedObject(this);
                    scheme.Initialize(outputPins.Cast<IPin>().ToArray(), inputPins.Cast<IPin>().ToArray(), loopback);
                }

                return m_Scheme;
            }
        }

        public override SocketScheme socketScheme => scheme;

        protected virtual void OnEnable()
        {
            if (isGlobal)
            {
                Registry.Add(this);
                OnSocketRegistryChange?.Invoke();
            }
        }

        protected virtual void OnDisable()
        {
            if (isGlobal)
            {
                Registry.Remove(this);
                OnSocketRegistryChange?.Invoke();
            }
            m_Scheme?.Dispose();
        }

        public Action Connect(IPlug<TScheme> plug)
        {
            var disconnect = scheme.ConnectPlug(plug.scheme);
            #if DEBUG
            /* Validate connection */
            if (plug.scheme.GetConnectedInScheme() != scheme)
            {
                throw new Exception($"Plug is connected to socket after explicit connection");
            }
            #endif
            plug.LockOnSocket(this);
            return disconnect;
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

        public virtual void OnBeforeSerialize()
        {
            Array.Resize(ref inputPins, scheme.inputTypes.Length);
            Array.Resize(ref outputPins, scheme.outputTypes.Length);
        }

        public virtual void OnAfterDeserialize()
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