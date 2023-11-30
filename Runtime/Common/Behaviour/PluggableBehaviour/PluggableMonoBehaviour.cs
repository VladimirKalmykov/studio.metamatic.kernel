using Sirenix.OdinInspector;

namespace Metamatic {
    using UnityEngine;

    public class PluggableMonoBehaviour: MonoBehaviour
    {
        [LabelText("Name")]
        public string hostName;
    }
}