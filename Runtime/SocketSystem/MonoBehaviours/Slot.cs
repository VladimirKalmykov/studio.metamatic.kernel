using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Metamatic.DevTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Metamatic.SocketSystem
{
    public abstract class Slot: MonoBehaviour {
        private static readonly Color DisconnectedGizmoColor = new(0.77f, 0.21f, 0.12f);
        private static readonly Color ConnectedGizmoColor = new(0.12f, 0.77f, 0.21f);
        public readonly List<Module> connectedModules = new();
        public readonly Dictionary<Module, SignalSource> connectionSignals = new();
        [SerializeField] public Vector3 anchorPosition;
        [SerializeField] public Quaternion anchorRotation;
        [SerializeField] protected bool allowMultipleConnections = true;
        /// <summary>
        /// Slot can have only one children. It will remove any children when connect module.
        /// </summary>
        [SerializeField] protected bool dedicated;
        
        public bool isSupportMultiConnections => allowMultipleConnections && !dedicated;
        
        #region Events
        public event Action OnReconnect;
        #endregion
        
        public Action Connect(Module module)
        {
#if DEBUG
            if (!Application.isPlaying)
            {
                throw new Exception("Slot can be connected only in play mode");
            }
#endif
            if (connectedModules.Contains(module))
            {
                throw new Exception("Module is already connected to this slot");
            }
            if (module.connectedSlot != null && module.connectedSlot != this)
            {
                throw new Exception($"Module {module} is already connected to slot {module.connectedSlot}");
            }

            InternalConnect(module);
            if (module.connectedSlot == null)
            {
                module.Connect(this);
            }
            OnReconnect?.Invoke();

            return () =>
            {
                Disconnect(module);
            };
        }
        
        public void Disconnect(Module module)
        {
#if DEBUG
            if (!Application.isPlaying)
            {
                throw new Exception("Slot can be disconnected only in play mode");
            }
#endif
            if (!connectedModules.Contains(module))
            {
                throw new Exception($"Module {module} is not connected to slot {this}");
            }
            
            InternalDisconnect(module);
            if (module.connectedSlot == this)
            {
                module.Disconnect(this);
            }
            OnReconnect?.Invoke();
        }
        protected abstract Action InternalConnect(Module module);
        protected abstract void InternalDisconnect(Module module);
        
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var size = 0.5f;
            var t = transform;
            var handleSize = HandleUtility.GetHandleSize(t.position);
            var radius = 0.2f * handleSize;
            var color = connectedModules.Count > 0 ? ConnectedGizmoColor : DisconnectedGizmoColor;
            Vector3 worldAnchorPosition = t.TransformPoint(anchorPosition);
            Quaternion worldAnchorRotation = t.rotation * anchorRotation;

            GizmoHelpers.DrawOrientation(t, worldAnchorPosition, worldAnchorRotation, size, color);
        }
#endif
        
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            var color = connectedModules.Count > 0 ? ConnectedGizmoColor : DisconnectedGizmoColor;
            Vector3 worldAnchorPosition = transform.TransformPoint(anchorPosition);
            var handleSize = HandleUtility.GetHandleSize(transform.position);
            var textColor = new GUIStyle();
            textColor.normal.textColor = color;
            UnityEditor.Handles.Label(worldAnchorPosition + (Vector3.right * 0.33f * handleSize) + (Vector3.up * 0.33f * handleSize), $"{GetType().Name}<{MiscUtils.GetGenericName(GetType().BaseType, true, true)}>", textColor);
        }
#endif  
    }
    
    public abstract class Slot<TFormFactor>: Slot where TFormFactor: FormFactor
    {
        public virtual UniTaskVoid OnConnected(Module<TFormFactor> module, Signal disconnectSignal)
        {
            return new UniTaskVoid();
        }
        
        protected override Action InternalConnect(Module module)
        {
            var moduleAsGeneric = module as Module<TFormFactor>;
            if (moduleAsGeneric == null)
            {
                throw new Exception($"Module {module} is not of type {typeof(TFormFactor)}");
            }
            return InternalConnect(moduleAsGeneric);
        }
        
        private Action InternalConnect(Module<TFormFactor> module)
        {
            if (connectedModules.Contains(module))
            {
                Debug.LogWarning("Module is already connected to this slot");
                return () => InternalDisconnect(module);
            }

            if (!allowMultipleConnections && connectedModules.Count > 0)
            {
                Debug.LogError("Multiple connections are not allowed");
                return () => { };
            }
            /* Remove any children except module */
            if (dedicated)
            {
                /* Disconnect all modules */
                InternalDisconnectAll();
                
                /* Remove rest of the children */
                for (var i = 0; i < transform.childCount; i++)
                {
                    var child = transform.GetChild(i);
                    if (child != module.transform && !connectedModules.Contains(child.GetComponent<Module>()))
                    {
                        Destroy(child.gameObject);
                    }
                }
            }
            
            connectedModules.Add(module);
            var signalSource = SignalSource.Create();
            connectionSignals.Add(module, signalSource);
            OnConnected(module, signalSource.Signal).Forget();
            
            return () => InternalDisconnect(module);
        }

        private void InternalDisconnect(Module<TFormFactor> module)
        {
            if (!connectedModules.Contains(module))
            {
                throw new Exception($"Module {module} is not connected to slot {this}");
            }

            if (connectionSignals.TryGetValue(module, out var signalSource))
            {
                signalSource.TryRaise();
            }
            connectedModules.Remove(module);
        }

        protected override void InternalDisconnect(Module module)
        {
            var moduleAsGeneric = module as Module<TFormFactor>;
            if (moduleAsGeneric == null)
            {
                throw new Exception($"Module {module} is not of type {typeof(TFormFactor)}");
            }
            InternalDisconnect(moduleAsGeneric);
        }
        
        protected void InternalDisconnectAll()
        {
            foreach (var module in connectedModules.ToArray())
            {
                InternalDisconnect(module);
            }
        }
    }
}