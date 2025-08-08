using System;
using UnityEngine;

namespace Metamatic.SocketSystem
{
    public abstract class Module : MonoBehaviour
    {
        /// <summary>
        /// Automatically connect to the first available slot in parent game object
        /// </summary>
        [SerializeField] protected bool autoConnect = true;

        /// <summary>
        /// Apply relative position and rotation to the module when it is connected to the slot
        /// </summary>
        [SerializeField] protected bool sticky = true;
        
        /// <summary>
        /// Position relative to the slot
        /// </summary>
        [SerializeField] protected Vector3 relativePosition;
        /// <summary>
        /// Rotation relative to the slot
        /// </summary>
        [SerializeField] protected Quaternion relativeRotation;
        
        #region Events
        public event Action OnReconnect;
        #endregion
        
        #region Private protected properties
        
        protected bool m_Destroyed;
        #endregion
        
        public Slot connectedSlot { get; protected set; }

        protected void EmitReconnect()
        {
            OnReconnect?.Invoke();
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
            EmitReconnect();
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
        
        public Action Connect(Slot slot)
        {
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
            EmitReconnect();

            return () =>
            {
                Disconnect(slot);
            };
        }
        
        public abstract bool TryFindAppropriateSlotInParent(out Slot slot);
        
        #region Mono
        private void OnDestroy()
        {
            m_Destroyed = true;
            Disconnect();
        }
        #endregion
    }
    public abstract class Module<TFormFactor>: Module where TFormFactor: FormFactor
    {
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
            if (TryFindAppropriateSlotInParent(out var slot))
            {
                Disconnect();
                Connect(slot);
            }
        }
        
        public override bool TryFindAppropriateSlotInParent(out Slot slot)
        {
            var parent = transform.parent;
            if (parent == null)
            {
                slot = null;
                return false;
            }
            slot = sticky ? parent.GetComponent<Slot<TFormFactor>>() : parent.GetComponentInParent<Slot<TFormFactor>>();
            if (slot == null)
            {
                return false;
            }
            return true;
        }
    }
}