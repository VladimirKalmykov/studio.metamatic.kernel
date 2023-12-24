using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;

namespace Metamatic {
    using UnityEngine;

    [ExecuteAlways]
    public abstract class ModuleLegacy: MonoBehaviour
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

        public SlotLegacy slotLegacy => GetConnectedSlot();
        protected virtual SlotLegacy GetConnectedSlot()
        {
            return null;
        }

        public ModuleAttachment attachment { get; protected set; } = null;

        public ModuleAttachment PlugIn(SlotLegacy slotLegacy)
        {
            return _PlugInFromBase(slotLegacy);
        }

        public void Unplug(SlotLegacy slotLegacy = null, SlotLegacy.UnplugReason reason = SlotLegacy.UnplugReason.Unknown)
        {
            _UnplugFromBase(slotLegacy, reason);
        }

        protected abstract ModuleAttachment _PlugInFromBase(SlotLegacy slotLegacy);
        protected abstract void _UnplugFromBase(SlotLegacy slotLegacy, SlotLegacy.UnplugReason reason = SlotLegacy.UnplugReason.Unknown);

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

        protected virtual void OnPlugInSlot(SlotLegacy slotLegacy)
        {
            
        }
        
        protected virtual void OnUnplugSlot(SlotLegacy slotLegacy)
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

    public class ModuleLegacy<TSlot> : ModuleLegacy
    where TSlot: SlotLegacy
    {
        public bool debug;
        [BoxGroup("Module"), ShowInInspector, ReadOnly, GUIColor("GetSlotFieldColor"), PropertyOrder(-1)]
        private TSlot m_Slot;

        protected override SlotLegacy GetConnectedSlot()
        {
            return m_Slot;
        }
        
        public new ModuleAttachment PlugIn(SlotLegacy slotLegacy)
        {
            return slotLegacy.PlugIn(this, InternalPlugIn);
        }
        
        protected override ModuleAttachment _PlugInFromBase(SlotLegacy slotLegacy)
        {
            return PlugIn(slotLegacy);
        }
        
        protected override void _UnplugFromBase(SlotLegacy slotLegacy, SlotLegacy.UnplugReason reason = SlotLegacy.UnplugReason.Unknown)
        {
            this.Unplug(slotLegacy, reason);
        }
        
        /// <summary>
        /// For internal use only
        /// </summary>
        /// <param name="slotLegacy"></param>
        /// <param name="attachment"></param>
        private void InternalPlugIn(SlotLegacy slotLegacy, ModuleAttachment attachment)
        {
            this.m_Slot = slotLegacy as TSlot;
            this.enabled = true;
            this.attachment = attachment;
            OnPlugInSlot(slotLegacy);
            Stick();
        }

        public new void Unplug(SlotLegacy unplugSlotLegacy = null, SlotLegacy.UnplugReason reason = SlotLegacy.UnplugReason.Unknown)
        {
            if (unplugSlotLegacy != null) unplugSlotLegacy.Unplug(this, InternalUnplug, reason);
        }

        public async Task<bool> InternalUnplug(SlotLegacy unplugSlotLegacy, SlotLegacy.UnplugReason reason = SlotLegacy.UnplugReason.Unknown)
        {
            if (unplugSlotLegacy == null)
            {
                throw new Exception("Slot is null");
            }

            if (this.slotLegacy == null)
            {
                throw new Exception("This slot is null");
            }
            if (unplugSlotLegacy != this.slotLegacy)
            {
                throw new Exception($"Module you are trying to unplug does not match the slot plugged to this module. unplugSlot: ${unplugSlotLegacy.gameObject.name}, this.slot: ${this.slotLegacy.gameObject.name}");
            }
            
            OnUnplugSlot(this.slotLegacy);
            
            m_Slot = null;
            if (unplugSlotLegacy.virtualSlot) return true;
            switch (reason)
            {
                case SlotLegacy.UnplugReason.ModuleDisable:
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
            if (slotLegacy != null)
            {
                slotLegacy.Unplug(this, SlotLegacy.UnplugReason.ModuleDisable);
            }
        }
        
        protected override void OnPlugInSlot(SlotLegacy slotLegacy)
        {
            this.m_Slot = (TSlot) slotLegacy;
        }
        
        protected override void OnUnplugSlot(SlotLegacy slotLegacy)
        {
            this.m_Slot = null;
        }

        private Color GetSlotFieldColor()
        {
            return slotLegacy == null ? Color.gray : Color.cyan;
        }
    }
}