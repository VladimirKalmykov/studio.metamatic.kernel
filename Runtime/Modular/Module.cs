using System;
using Sirenix.OdinInspector;

namespace Metamatic {
    using UnityEngine;

    [ExecuteAlways]
    public class Module: MonoBehaviour {
        [BoxGroup("Module")]
        public bool sticky = true;
        [BoxGroup("Module"), ShowIf("sticky")]
        public Vector3 position;
        [BoxGroup("Module"), ShowIf("sticky")]
        public Quaternion rotation;
        [BoxGroup("Module"), ShowIf("sticky")]
        [Tooltip("Update position in editor every frame. Otherwise, update only on validate")]
        public bool editorLiveUpdate = true;

        public Slot slot { get; private set; }

        void Start() {
            var parent = transform.parent;

            Stick();
        }

        void OnEnable()
        {
            OnModuleEnable();
        }

        void OnDisable()
        {
            OnModuleDisable();
        }

        protected virtual void OnModuleEnable()
        {
            if (slot == null)
            {
                var parentSlot = transform.parent != null ? transform.parent.GetComponent<Slot>() : null;
                if (parentSlot != null)
                {
                    parentSlot.PlugIn(this);
                }
            }
        }

        protected virtual void OnModuleDisable()
        {
            
        }

        public void Stick() {
            var parent = transform.parent;

            if (sticky && parent != null) {
                transform.localPosition = position;
                transform.localRotation = rotation;
            }
        }

        public ModuleAttachment PlugIn(Slot slot)
        {
            return slot.PlugIn(this, InternalPlugIn);
        }
        
        /// <summary>
        /// For internal use only
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="attachment"></param>
        private void InternalPlugIn(Slot slot)
        {
            this.slot = slot;
            this.enabled = true;
            OnPlugInSlot(slot);
            Stick();
        }

        public void Unplug(Slot slot = null)
        {
            if (slot != null) slot.Unplug(this, InternalUnplug);
        }

        public void InternalUnplug(Slot slot)
        {
            if (slot != this.slot)
            {
                throw new Exception("Module you are trying to unplug does not match the slot plugged to this module");
            }
            
            OnUnplugSlot(this.slot);
            
            this.slot = null;
            gameObject.SetActive(false);
            transform.SetParent(null);
            InstanceOrigin.Release(this);
        }

        protected virtual void OnPlugInSlot(Slot slot)
        {
            
        }
        
        protected virtual void OnUnplugSlot(Slot slot)
        {
            
        }

        #if UNITY_EDITOR
        void Update() {
            if (sticky && editorLiveUpdate && !Application.isPlaying) {
                transform.localPosition = position;
                transform.localRotation = rotation;
            }
        }

        void OnValidate() {
            if (sticky) {
                transform.localPosition = position;
                transform.localRotation = rotation;
            }
        }
        #endif
    }

    public class Module<TSlot> : Module
    where TSlot: Slot
    {
        [BoxGroup("Module"), ShowInInspector, ReadOnly, GUIColor("GetSlotFieldColor"), PropertyOrder(-1)]
        protected new TSlot slot;
        public void PlugIn(TSlot slot)
        {
            base.PlugIn(slot);
        }
        
        protected override void OnModuleEnable()
        {
            if (slot == null)
            {
                var parentSlot = transform.parent != null ? transform.parent.GetComponent<Slot>() : null;
                if (parentSlot != null)
                {
                    if (parentSlot is TSlot)
                    {
                        this.PlugIn(parentSlot);
                    }
                    else
                    {
                        Debug.LogError("Slot type and module's requirements are incompatible");
                    }
                }
            }
        }
        
        protected override void OnPlugInSlot(Slot slot)
        {
            this.slot = (TSlot) slot;
        }
        
        protected override void OnUnplugSlot(Slot slot)
        {
            this.slot = null;
        }

        private Color GetSlotFieldColor()
        {
            return slot == null ? Color.red : Color.cyan;
        }
    }
}