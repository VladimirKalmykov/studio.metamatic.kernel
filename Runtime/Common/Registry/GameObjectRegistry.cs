using System;

namespace Metamatic {
    using UnityEngine;
    [Obsolete("Use Dispatcher events instead", true)]
    [CreateAssetMenu(fileName = "GameObjectRegistry", menuName = "Metamatic/Registry/Game Object", order = 1)]  
    public class GameObjectRegistry: ScriptableRegistry<GameObject> {}
}