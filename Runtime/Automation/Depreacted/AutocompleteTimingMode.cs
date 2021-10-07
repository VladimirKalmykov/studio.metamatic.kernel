using System;

namespace Metamatic {
    [Serializable]
    public enum AutomationTimingMode {
        /// <summary>
        /// Runtime timer will complete the task after duration time will expire
        /// </summary>
        Autocomplete,
        /// <summary>
        /// Runtime timer will cancel the task after duration time will expire
        /// </summary>
        Autocancel,
        /// <summary>
        /// Runtime timer will throw the task after duration time will expire
        /// </summary>
        Autothrow,
        /// <summary>
        /// Runtime timer go nothing after duration time will expire
        /// </summary>
        Passive,
        /// <summary>
        /// Runtime timer is disabled, but task will autocomplete immediatelly
        /// </summary>
        SideEffect,
        /// <summary>
        /// Runtime timer is disabled
        /// </summary>
        Disabled,
        /// <summary>
        /// The timer runs, but it is infinite
        /// </summary>
        Endless
    }
}