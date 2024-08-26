using System;

namespace Metamatic {
    using UnityEngine;
    [Obsolete("Use Dispatcher events instead", true)]
    [CreateAssetMenu(fileName = "TransformRegistry", menuName = "Metamatic/Registry/Tramsform", order = 1)]  
    public class TransformRegistry: ScriptableRegistry<Transform> {}
}