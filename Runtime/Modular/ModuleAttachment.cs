using System;

namespace Metamatic
{
    /// <summary>
    /// Allow to control module to slot attachment and release attachment manually. To get module attachment you may call Module attach method with providing module to attach.
    /// </summary>
    public class ModuleAttachment
    {
        public readonly ModuleLegacy moduleLegacy;
        public readonly SlotLegacy slotLegacy;
        public bool released = false;
        public event Action OnRelease;

        public ModuleAttachment(SlotLegacy slotLegacy, ModuleLegacy moduleLegacy)
        {
            this.moduleLegacy = moduleLegacy;
            this.slotLegacy = slotLegacy;
            
            /*
             * Auto connect sockets
             */
            var slotSockets = slotLegacy.GetSockets();
            foreach (var slotSocket in slotSockets)
            {
                slotSocket.AutoAttach(this.moduleLegacy.gameObject, true);
            }
        }
        
        /// <summary>
        /// Release module attachment and destroy module if it was created from prefab
        /// </summary>
        public void Release()
        {
            released = true;
            /*
             * Auto disconnect sockets
             */
            var slotSockets = slotLegacy.GetSockets();
            foreach (var slotSocket in slotSockets)
            {
                slotSocket.DetachFrom(moduleLegacy.gameObject);
            }
            OnRelease?.Invoke();
            moduleLegacy.Unplug(slotLegacy);
        }
    }
}