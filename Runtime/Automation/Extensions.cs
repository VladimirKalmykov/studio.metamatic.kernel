namespace Metamatic {
    using UnityEngine;

    public static class AutomationExtensionMethods {
        /* Get nearest parent component except self */
        public static AutomationTrack GetAutomationTrack(this MonoBehaviour component, string name = null) {
            if (name == null) {
                var track = component.GetComponent<AutomationTrack>();
                if (track == null) {
                    return component.gameObject.AddComponent<AutomationTrack>();
                }
            } else {
                var tracks = component.GetComponents<AutomationTrack>();
                foreach(var track in tracks) {
                    if (track.name == name) {
                        return track;
                    }
                }
            }

            var newTrack = component.gameObject.AddComponent<AutomationTrack>();
            newTrack.name = name;
            return newTrack;
        }
    }
}