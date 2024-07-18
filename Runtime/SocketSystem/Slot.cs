using System;
using System.Collections.Generic;
using Metamatic.DevTools;
using UnityEditor;
using UnityEngine;

namespace Metamatic
{
    public abstract class Slot: MonoBehaviour
    {
        private static readonly Color DisconnectedGizmoColor = new(0.77f, 0.21f, 0.12f);
        private static readonly Color ConnectedGizmoColor = new(0.12f, 0.77f, 0.21f);
        public Socket[] sockets;
        [SerializeField] private bool allowMultipleConnections;
        /// <summary>
        /// Slot can have only one children. It will remove any children when connect module.
        /// </summary>
        [SerializeField] protected bool dedicated;
        [SerializeField] private Vector3 m_AnchorPosition;
        [SerializeField] private Quaternion m_AnchorRotation;
        public Vector3 anchorPosition
        {
            get => m_AnchorPosition;
            set => m_AnchorPosition = value;
        }
        public Quaternion anchorRotation
        {
            get => m_AnchorRotation;
            set => m_AnchorRotation = value;
        }
        public readonly List<Module> connectedModules = new();
        public abstract bool isSupportMultiConnections { get; }
        protected abstract Action InternalConnect(Module module);
        protected abstract void InternalDisconnect(Module module);
        public event Action OnReconnect;
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

        public abstract FormFactorSocketInfo[] GetSocketSchemesTypes();
        
        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var size = 0.5f;
            var t = transform;
            var handleSize = HandleUtility.GetHandleSize(t.position);
            var radius = 0.2f * handleSize;
            var color = connectedModules.Count > 0 ? ConnectedGizmoColor : DisconnectedGizmoColor;
            Vector3 worldAnchorPosition = t.TransformPoint(m_AnchorPosition);
            Quaternion worldAnchorRotation = t.rotation * m_AnchorRotation;

            GizmoHelpers.DrawOrientation(t, worldAnchorPosition, worldAnchorRotation, size, color);
        }
        #endif
        
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            var color = connectedModules.Count > 0 ? ConnectedGizmoColor : DisconnectedGizmoColor;
            Vector3 worldAnchorPosition = transform.TransformPoint(m_AnchorPosition);
            var handleSize = HandleUtility.GetHandleSize(transform.position);
            var textColor = new GUIStyle();
            textColor.normal.textColor = color;
            UnityEditor.Handles.Label(worldAnchorPosition + (Vector3.right * 0.33f * handleSize) + (Vector3.up * 0.33f * handleSize), $"{GetType().Name}<{MiscUtils.GetGenericName(GetType().BaseType, true, true)}>", textColor);
        }
#endif  
    }
    
    public class Slot<TFormFactor> : Slot, ISerializationCallbackReceiver
        where TFormFactor: FormFactor, new()
    {
        private TFormFactor m_FormFactor;
        private FormFactorSocketSchemeInfo[] m_CachedSocketSchemes;
        private FormFactorSocketInfo[] m_CachedSockets;
        public FormFactorSocketSchemeInfo[] socketSchemes
        {
            get
            {
                if (m_CachedSocketSchemes == null)
                {
                    m_CachedSocketSchemes = FormFactor.GetSocketSchemeTypes(typeof(TFormFactor));
                }
                return m_CachedSocketSchemes;
            }
        }
        public FormFactorSocketInfo[] socketsInfo
        {
            get
            {
                if (m_CachedSockets == null)
                {
                    m_CachedSockets = FormFactor.GetSocketTypes(typeof(TFormFactor));
                }
                return m_CachedSockets;
            }
        }
        
        public void OnBeforeSerialize()
        {
            Array.Resize(ref sockets, socketSchemes.Length);
        }

        public void OnAfterDeserialize()
        {
            Array.Resize(ref sockets, socketSchemes.Length);
        }
        
        private Action InternalConnect(Module<TFormFactor> module)
        {
            if (connectedModules.Contains(module))
            {
                Debug.LogWarning("Module is already connected to this slot");
                return () => InternalDisconnect(module);
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
            
            if (connectedModules.Count != 0 && !isSupportMultiConnections)
            {
                throw new Exception($"Slot can not connect to the module {module}, because {this} is already connected to module {connectedModules[0]}");
            }
            connectedModules.Add(module);
            for (var i = 0; i < sockets.Length; i++)
            {
                sockets[i].Connect(module.plugs[i]);
            }
            
            return () => InternalDisconnect(module);
        }

        public override bool isSupportMultiConnections => IsSupportMultipleConnections();

        private bool IsSupportMultipleConnections()
        {
            return false; // TODO: Implement
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

        private void InternalDisconnect(Module<TFormFactor> module)
        {
            if (!connectedModules.Contains(module))
            {
                throw new Exception($"Module {module} is not connected to slot {this}");
            }
            for (var i = 0; i < sockets.Length; i++)
            {
                sockets[i].Disconnect(module.plugs[i]);
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
            for (var i = 0; i < connectedModules.Count; i++)
            {
                var module = connectedModules[i];
                for (var j = 0; j < sockets.Length; j++)
                {
                    sockets[j].Disconnect(module.plugs[j]);
                }
            }
            connectedModules.Clear();
        }

        public override FormFactorSocketInfo[] GetSocketSchemesTypes()
        {
            return socketsInfo;
        }
    }
}