using System;
using Sirenix.OdinInspector;

namespace Metamatic {
    using UnityEngine;

    [ExecuteAlways]
    public class Module: Element {
        [BoxGroup("Module")]
        public bool sticky = true;
        [BoxGroup("Module"), ShowIf("sticky")]
        public Vector3 position;
        [BoxGroup("Module"), ShowIf("sticky")]
        public Quaternion rotation;
        [BoxGroup("Module"), ShowIf("sticky")]
        [Tooltip("Update position in editor every frame. Otherwise, update only on validate")]
        public bool editorLiveUpdate = true;

        public Slot slot { get; protected set; }

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

        public ModuleAttachment PlugIn(Slot slot, ModuleAttachment attachment = null)
        {
            if (gameObject.scene.name == null || gameObject.scene.name == gameObject.name)
            {
                throw new Exception("Module you trying to attach is a prefab");
            }
            this.slot = slot;
            OnPlugInSlot(slot);
            transform.SetParent(slot.transform, false);
            Stick();
            if (attachment == null)
            {
                return slot.PlugIn(this, true);
            }

            return attachment;
        }

        public void Unplug(Slot slot = null, ModuleAttachment attachment = null)
        {
            if (slot == this.slot)
            {
                throw new Exception("Module you are trying to unplug does not match the slot plugged to this module");
            }

            if (slot != null && attachment == null)
            {
                slot.Unplug(this, true);
            }
            OnUnplugSlot(this.slot);
            
            this.slot = null;
            if (this == null) return;
            transform.SetParent(null);
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