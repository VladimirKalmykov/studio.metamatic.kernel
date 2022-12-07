namespace Metamatic {
    using UnityEngine;

    [CreateAssetMenu(fileName = "EntityClass", menuName = "Metamatic/Nature/Entity Class", order = 1)]
    public class EntityClass: ScriptableObject {
        public EntityClass baseClass;
        public string title = "Unknown class";
    }
}