using System;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metamatic
{
    public enum AutoConnectStrategies
    {
        /// <summary>
        /// Auto connect is disabled.
        /// </summary>
        Disabled,
        /// <summary>
        /// Auto connect to the first socket that matches the plug in the parent hierarchy.
        /// </summary>
        Hierarchical,
        /// <summary>
        /// Auto connect to the first socket that matches the plug in the scene.
        /// </summary>
        Global
    }
    public interface IPlug
    {
        
    }

    public interface IPlug<TScheme>: IPlug
    where TScheme: SocketScheme
    {
        public TScheme scheme { get; }
        /// <summary>
        /// Lock to the connected socket. It can not change the socket by hierarchy or global changes.
        /// </summary>
        /// <param name="socket"></param>
        public void LockOnSocket(ISocket<TScheme> socket);
    }
    
    public abstract class Plug: MonoBehaviour, IPlug
    {
        #region Fields
        public Pin[] outputPins;
        public Pin[] inputPins;
        /// <summary>
        /// Auto connect to socket using few different strategies. 
        /// </summary>
        [SerializeField] protected AutoConnectStrategies autoConnect = AutoConnectStrategies.Disabled;
        /// <summary>
        /// Auto connect to socket witch has the same name as the plug.
        /// </summary>
        [SerializeField] protected bool autoConnectByName;

        [SerializeField] protected string socketName;
        #endregion

        /// <summary>
        /// Plug was auto locked by auto connect strategy. It can not change the socket by hierarchy or global changes.
        /// </summary>
        protected MicroState<bool> m_AutoLocked = new(false);
        public MicroStateReadOnly<bool> autoLocked => m_AutoLocked;
        public Type[] inputTypes => GetInputTypes();
        public Type[] outputTypes => GetOutputTypes();
        public Type[] inputPinTypes => GetInputPinTypes();
        public Type[] outputPinTypes => GetOutputPinTypes();
        protected abstract Type[] GetInputTypes();
        protected abstract Type[] GetOutputTypes();
        protected abstract Type[] GetInputPinTypes();
        protected abstract Type[] GetOutputPinTypes();
        public abstract SocketScheme socketScheme { get; }
        private void OnTransformParentChanged() {
            Debug.Log($"Transform parent changed! {name}, {autoConnect}");
            if (autoConnect == AutoConnectStrategies.Hierarchical)
            {
                AutoConnectToParent();
            }
        }
        protected abstract void AutoConnectToParent();
    }
    
    public abstract class Plug<TScheme>: Plug, IPlug<TScheme>, ISerializationCallbackReceiver
    where TScheme: SocketScheme, new()
    {
        [ShowInInspector, ReadOnly]
        private Socket<TScheme> m_Socket;
        private TScheme m_Scheme;
        public TScheme scheme => m_Scheme ??= new TScheme();
        public void LockOnSocket(ISocket<TScheme> socket)
        {
            if (scheme.GetConnectedInScheme() == socket.scheme)
            {
                m_AutoLocked.value = true;
            }
        }

        public override SocketScheme socketScheme => scheme;
        
        protected virtual void OnEnable()
        {
            scheme.debug_SetRelatedObject(this);
            /*
             * Initialize the scheme with the input and output pins, but switch the input and output pins because plug has reversed input and output pins.
             */
            scheme.Initialize(inputPins.Cast<IPin>().ToArray(), outputPins.Cast<IPin>().ToArray());
            scheme.OnReconnectIn += OnSchemeInputReconnect;
            if (autoConnect == AutoConnectStrategies.Hierarchical)
            {
                AutoConnectToParent();
            } else if (autoConnect == AutoConnectStrategies.Global)
            {
                SubscribeAndAutoConnectToGlobal();
            }
        }

        private void OnSchemeInputReconnect(bool connected)
        {
            if (connected) return;
            /* Disable auto lock when input is disconnected. */
            m_AutoLocked.value = false;
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
        
        /// <summary>
        /// Auto connect to the first socket that matches the plug in the parent hierarchy.
        /// </summary>
        protected override void AutoConnectToParent() {
            if (transform.parent == null)
            {
                AutoDisconnectFromParent();
                return;
            }
            if (autoConnectByName)
            {
                AutoConnectToParentByName();
            }
            else
            {
                AutoConnectToParentByType();
            }
        }

        private void AutoDisconnectFromParent()
        {
            if (m_AutoLocked.value)
            {
                return;
            }
            var inScheme = scheme.GetConnectedInScheme();
            if (inScheme == null)
            {
                return;
            }
            inScheme.DisconnectPlug(scheme);
        }
        
        private void AutoConnectToParentByName()
        {
            if (m_AutoLocked.value)
            {
                return;
            }
            var parentSockets = transform.parent.GetComponentsInParent<Socket<TScheme>>();
           
            if (parentSockets.Length == 0)
            {
                AutoDisconnectFromParent();
                return;
            }
            var parentSocket = parentSockets.FirstOrDefault(socket => socket.socketName == socketName);
            if (parentSocket == null)
            {
                AutoDisconnectFromParent();
                return;
            }
            var inScheme = scheme.GetConnectedInScheme();
            if (parentSocket.scheme == inScheme)
            {
                return;
            }
            inScheme?.DisconnectPlug(scheme);
            parentSocket.Connect(this);
        }
        
        private void AutoConnectToParentByType()
        {
            if (m_AutoLocked.value)
            {
                return;
            }
            var parentSocket = transform.parent.GetComponentInParent<Socket<TScheme>>();
            if (parentSocket == null)
            {
                AutoDisconnectFromParent();
                return;
            }
            /* Compare to currently connected socket. It should be different to continue. */
            var inScheme = scheme.GetConnectedInScheme();
            if (parentSocket.scheme == inScheme)
            {
                return;
            }
            /* Disconnect from the current socket. */
            inScheme?.DisconnectPlug(scheme);

            /* Connect to the parent socket. */
            parentSocket.Connect(this);
        }
        
        /// <summary>
        /// Auto connect to the first global socket that matches the plug in the scene.
        /// </summary>
        private void SubscribeAndAutoConnectToGlobal()
        {
            if (m_AutoLocked.value)
            {
                return;
            }
            
            Socket<TScheme>.OnSocketRegistryChange += AutoConnectToGlobal;
            AutoConnectToGlobal();
        }

        private void AutoConnectToGlobal()
        {
            Socket<TScheme> globalSocket;
            if (autoConnectByName)
            {
                globalSocket = Socket<TScheme>.GetByName(socketName);
            }
            else
            {
                globalSocket = Socket<TScheme>.Registry.FirstOrDefault();
            }
            if (globalSocket == null)
            {
                return;
            }
            
            var inScheme = scheme.GetConnectedInScheme();
            if (globalSocket.scheme == inScheme)
            {
                return;
            }
            inScheme?.DisconnectPlug(scheme);
            globalSocket.Connect(this);
        }
    }
}