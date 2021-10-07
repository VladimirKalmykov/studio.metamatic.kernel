using System;

namespace Metamatic {
    public static partial class Automation {
        [Serializable]
        public enum OverlapMode {
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
            Cancel,
            /// <summary>
            /// Add next script to the queue
            /// </summary>
            Sequence
        }
    }
}