using System;

namespace Metamatic {
    /// <summary>
    /// Describes that class is plugin for one or the several another classes
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class PluginFor: Attribute {
        public Type[] types;
        public PluginFor(params Type[] types) {
            if (types.Length == 0) {
                throw new Exception("Plugin must be for at least one compatible type");
            }
            this.types = types;
        }
    }
}