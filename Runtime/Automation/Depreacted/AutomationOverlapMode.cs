using System;

namespace Metamatic {
    [Serializable]
    public enum AutomationOverlapMode {
        /// <summary>
        /// No parallel runtime limitations
        /// </summary>
        Unlimit,
        /// <summary>
        /// The next runtime can not be played until the current does
        /// </summary>
        Block,
        /// <summary>
        /// Forwards current runtime to play next
        /// </summary>
        Forwards,
        /// <summary>
        /// Cancel current runtime to play next
        /// </summary>
        Cancel
    }
}