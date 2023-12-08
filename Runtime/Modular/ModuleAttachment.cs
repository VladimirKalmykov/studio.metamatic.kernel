using System;

namespace Metamatic
{
    /// <summary>
    /// Allow to control module to slot attachment and release attachment manually. To get module attachment you may call Module attach method with providing module to attach.
    /// </summary>
    public class ModuleAttachment
    {
        public readonly Module module;
        public readonly Slot slot;
        public bool released = false;
        public event Action OnRelease;

        public ModuleAttachment(Slot slot, Module module)
        {
            this.module = module;
            this.slot = slot;
            
            /*
             * Auto connect sockets
             */
            var slotSockets = slot.GetSockets();
            foreach (var slotSocket in slotSockets)
            {
                slotSocket.AutoAttach(this.module.gameObject, true);
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
            var slotSockets = slot.GetSockets();
            foreach (var slotSocket in slotSockets)
            {
                slotSocket.DetachFrom(module.gameObject);
            }
            OnRelease?.Invoke();
            module.Unplug(slot);
        }
    }
}