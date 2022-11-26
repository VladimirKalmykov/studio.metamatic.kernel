namespace Metamatic {
    using UnityEngine;

    [CreateAssetMenu(fileName = "CharacterType", menuName = "Metamatic/Nature/Character Type", order = 1)]
    public class EntityClass: ScriptableObject {
        public EntityClass baseClass;
        public string title = "Unknown class";
    }
}