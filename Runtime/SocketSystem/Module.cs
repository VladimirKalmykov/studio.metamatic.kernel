using System;
using Metamatic.DevTools;
using UnityEngine;

namespace Metamatic
{
    public abstract class Module: MonoBehaviour
    {
        private static readonly Color DisconnectedGizmoColor = new(0.77f, 0.21f, 0.12f);
        private static readonly Color ConnectedGizmoColor = new(0.12f, 0.77f, 0.21f);
        public event Action OnReconnect;
        public Plug[] plugs;
        /// <summary>
        /// Automatically connect to the first available slot in parent game object
        /// </summary>
        [SerializeField] protected bool autoConnect;
        /// <summary>
        /// Apply relative position and rotation to the module when it is connected to the slot
        /// </summary>
        [SerializeField] protected bool sticky;
        /// <summary>
        /// Position relative to the slot
        /// </summary>
        [SerializeField] protected Vector3 relativePosition;
        /// <summary>
        /// Rotation relative to the slot
        /// </summary>
        [SerializeField] protected Quaternion relativeRotation;
        public Slot connectedSlot { get; protected set; }
        private bool m_Destroyed = false;
        public Action Connect(Slot slot)
        {
            Debug.Log($"Connect slot: {slot}");
#if DEBUG
            if (!Application.isPlaying)
            {
                throw new Exception("Module can be connected only in play mode");
            }
#endif
            if (connectedSlot == slot)
            {
                throw new Exception("Slot is already connected to this module");
            }
            connectedSlot = slot;
            if (!slot.connectedModules.Contains(this))
            {
                slot.Connect(this);
            }

            ApplyPostConnectionEffects();
            OnReconnect?.Invoke();

            return () =>
            {
                Disconnect(slot);
            };
        }

        public void Disconnect(Slot slot)
        {
#if DEBUG
            if (!Application.isPlaying)
            {
                throw new Exception("Module can be disconnected only in play mode");
            }
#endif
            if (connectedSlot != slot)
            {
                throw new Exception($"Slot {slot} is not connected to this module");
            }
            connectedSlot = null;
            if (slot.connectedModules.Contains(this))
            {
                slot.Disconnect(this);
            }
            ApplyPostDisconnectionEffects();
            OnReconnect?.Invoke();
        }

        public void Disconnect()
        {
            if (connectedSlot != null)
            {
                Disconnect(connectedSlot);
            }
        }

        /// <summary>
        /// Called when module is connected to the slot. Don't forget to call base method when overriding.
        /// </summary>
        protected virtual void ApplyPostConnectionEffects()
        {
            /*
             * Set parent to the slot
             */
            transform.SetParent(connectedSlot.transform, true);
            /*
             * Apply relative position and rotation to the module when it is connected to the slot
             */
            if (sticky)
            {
                var t = transform;
                t.localPosition = connectedSlot.anchorPosition + relativePosition;
                t.localRotation = Trigf.SumQuaternions(connectedSlot.anchorRotation, relativeRotation);
            }
        }
        /// <summary>
        /// Called when module is disconnected from the slot. Don't forget to call base method when overriding.
        /// </summary>
        protected virtual void ApplyPostDisconnectionEffects()
        {
            /* Return because we can not change parent during destroying */
            if (m_Destroyed) return;
            /*
             * Set parent to the slot
             */
            transform.SetParent(null, true);
        }
        public abstract FormFactorPlugInfo[] GetPlugTypes();

        public void OnDrawGizmosSelected()
        {
            var t = transform;
            Vector3 worldAnchorPosition = t.TransformPoint(relativePosition);
            Quaternion worldAnchorRotation = t.rotation * relativeRotation;
            var color = connectedSlot != null ? ConnectedGizmoColor : DisconnectedGizmoColor;

            GizmoHelpers.DrawOrientation(t, worldAnchorPosition, worldAnchorRotation, 0.5f, color);
        }

        private void OnDestroy()
        {
            m_Destroyed = true;
            Disconnect();
        }
    }
    
    public class Module<TFormFactor> : Module, ISerializationCallbackReceiver
        where TFormFactor : FormFactor, new()
    {
        private TFormFactor m_FormFactor;
        
        private FormFactorSocketSchemeInfo[] m_CachedSocketSchemes;
        private FormFactorPlugInfo[] m_CachedPlugs;
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
        public FormFactorPlugInfo[] plugsInfo
        {
            get
            {
                if (m_CachedPlugs == null)
                {
                    m_CachedPlugs = FormFactor.GetPlugTypes(typeof(TFormFactor));
                }
                return m_CachedPlugs;
            }
        }
        
        public void OnBeforeSerialize()
        {
            Array.Resize(ref plugs, socketSchemes.Length);
        }

        public void OnAfterDeserialize()
        {
            Array.Resize(ref plugs, socketSchemes.Length);
        }
        
        public override FormFactorPlugInfo[] GetPlugTypes()
        {
            return plugsInfo;
        }
        
        private void Start()
        {
            if (autoConnect)
            {
                AutoConnect();
            }
        }
        
        private void OnTransformParentChanged()
        {
            if (autoConnect)
            {
                Disconnect();
                AutoConnect();
            }
        }
        
        public void AutoConnect()
        {
            var parent = transform.parent;
            if (parent == null)
            {
                return;
            }
            var slot = sticky ? parent.GetComponent<Slot<TFormFactor>>() : parent.GetComponentInParent<Slot<TFormFactor>>();
            if (slot != null && connectedSlot != slot)
            {
                Disconnect();
                Connect(slot);
            }
        }
    }
}