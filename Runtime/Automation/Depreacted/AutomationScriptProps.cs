using System;

namespace Metamatic {
    [Serializable]
    public class AutomationScriptProps {
        public AutomationScriptProps Clone() {
            return (AutomationScriptProps)this.MemberwiseClone();
        }
    }
}