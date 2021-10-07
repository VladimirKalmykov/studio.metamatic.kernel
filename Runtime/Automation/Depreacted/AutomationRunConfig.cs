using System;

namespace Metamatic {
    [Serializable]
    public class AutomationRunConfig {
        public const float DEFAULT_DURATION = 1f;
        public float duration;
        public static AutomationRunConfig Duration(float duration) {
            return new AutomationRunConfig() {
                duration = duration
            };
        }
    }
}