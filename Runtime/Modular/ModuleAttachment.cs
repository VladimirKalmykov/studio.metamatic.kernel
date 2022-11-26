using UnityEngine;

namespace Metamatic
{
    /// <summary>
    /// Allow to control module to slot attachment and release attachment manually. To get module attachment you may call Module attach method with providing module to attach.
    /// </summary>
    public class ModuleAttachment
    {
        public readonly Module module;
        public readonly bool instantianted;
        public readonly Slot slot;

        public ModuleAttachment(Slot slot, Module module, bool instantianted)
        {
            this.module = module;
            this.instantianted = instantianted;
            this.slot = slot;
            
            module.PlugIn(slot);
        }
        
        /// <summary>
        /// Release module attachment and destroy module if it was created from prefab
        /// </summary>
        public void Release()
        {
            module.Unplug();
            if (instantianted)
            {
                Object.Destroy(module.gameObject);
            }
        }
    }
}