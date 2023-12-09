using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;

namespace Metamatic {
    using UnityEngine;

    [ExecuteAlways]
    public abstract class Module: MonoBehaviour
    {
        [BoxGroup("Module"), ShowInInspector, ReadOnly, PropertyOrder(0)] public string UUID { get; private set; } = "";
        #if UNITY_EDITOR
        [PropertyOrder(1)]
        [BoxGroup("Module")]
        [Button]
        void CopyUUID() {
            GUIUtility.systemCopyBuffer = UUID;
        }
        #endif
        [BoxGroup("Module"), PropertyOrder(2)]
        public bool sticky = true;
        [BoxGroup("Module"), ShowIf("sticky"), PropertyOrder(3)]
        public Vector3 position;
        [BoxGroup("Module"), ShowIf("sticky"), PropertyOrder(4)]
        public Quaternion rotation;
        [BoxGroup("Module"), ShowIf("sticky"), PropertyOrder(5)]
        [Tooltip("Update position in editor every frame. Otherwise, update only on validate")]
        public bool editorLiveUpdate = true;

        public Slot slot => GetConnectedSlot();
        protected virtual Slot GetConnectedSlot()
        {
            return null;
        }

        public ModuleAttachment attachment { get; protected set; } = null;

        public ModuleAttachment PlugIn(Slot slot)
        {
            return _PlugInFromBase(slot);
        }

        public void Unplug(Slot slot = null, Slot.UnplugReason reason = Slot.UnplugReason.Unknown)
        {
            _UnplugFromBase(slot, reason);
        }

        protected abstract ModuleAttachment _PlugInFromBase(Slot slot);
        protected abstract void _UnplugFromBase(Slot slot, Slot.UnplugReason reason = Slot.UnplugReason.Unknown);

        void Start() {
            var parent = transform.parent;

            Stick();
        }

        void OnEnable()
        {
            if (UUID.Length == 0)
            {
                UUID = Guid.NewGuid().ToString();
            }
            OnInternalModuleEnable();
            OnModuleEnable();
        }

        void OnDisable()
        {
            OnInternalModuleDisable();
            OnModuleDisable();
        }
        
        protected virtual void OnInternalModuleEnable()
        {
            
        }

        protected virtual void OnModuleEnable()
        {
            
        }
        
        protected virtual void OnInternalModuleDisable()
        {
            
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
        public bool debug;
        [BoxGroup("Module"), ShowInInspector, ReadOnly, GUIColor("GetSlotFieldColor"), PropertyOrder(-1)]
        private TSlot m_Slot;

        protected override Slot GetConnectedSlot()
        {
            return m_Slot;
        }
        
        public new ModuleAttachment PlugIn(Slot slot)
        {
            return slot.PlugIn(this, InternalPlugIn);
        }
        
        protected override ModuleAttachment _PlugInFromBase(Slot slot)
        {
            return PlugIn(slot);
        }
        
        protected override void _UnplugFromBase(Slot slot, Slot.UnplugReason reason = Slot.UnplugReason.Unknown)
        {
            this.Unplug(slot, reason);
        }
        
        /// <summary>
        /// For internal use only
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="attachment"></param>
        private void InternalPlugIn(Slot slot, ModuleAttachment attachment)
        {
            this.m_Slot = slot as TSlot;
            this.enabled = true;
            this.attachment = attachment;
            OnPlugInSlot(slot);
            Stick();
        }

        public new void Unplug(Slot unplugSlot = null, Slot.UnplugReason reason = Slot.UnplugReason.Unknown)
        {
            if (unplugSlot != null) unplugSlot.Unplug(this, InternalUnplug, reason);
        }

        public async Task<bool> InternalUnplug(Slot unplugSlot, Slot.UnplugReason reason = Slot.UnplugReason.Unknown)
        {
            if (unplugSlot == null)
            {
                throw new Exception("Slot is null");
            }

            if (this.slot == null)
            {
                throw new Exception("This slot is null");
            }
            if (unplugSlot != this.slot)
            {
                throw new Exception($"Module you are trying to unplug does not match the slot plugged to this module. unplugSlot: ${unplugSlot.gameObject.name}, this.slot: ${this.slot.gameObject.name}");
            }
            
            OnUnplugSlot(this.slot);
            
            m_Slot = null;
            if (unplugSlot.virtualSlot) return true;
            switch (reason)
            {
                case Slot.UnplugReason.ModuleDisable:
                    // Wait for the end of frame
                    await UniTask.WaitForEndOfFrame();
                    if (this == null)
                    {
                        /* Object was destroyed */
                        return true;
                    }
                    break;
            }
            gameObject.SetActive(false);
            transform.SetParent(null);
            InstanceOrigin.Release(this);

            return true;
        }
        
        protected override void OnInternalModuleEnable()
        {
           
        }
        
        protected override void OnInternalModuleDisable()
        {
            if (slot != null)
            {
                slot.Unplug(this, Slot.UnplugReason.ModuleDisable);
            }
        }
        
        protected override void OnPlugInSlot(Slot slot)
        {
            this.m_Slot = (TSlot) slot;
        }
        
        protected override void OnUnplugSlot(Slot slot)
        {
            this.m_Slot = null;
        }

        private Color GetSlotFieldColor()
        {
            return slot == null ? Color.gray : Color.cyan;
        }
    }
}