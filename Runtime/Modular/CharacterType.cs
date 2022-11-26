namespace Metamatic {
    using UnityEngine;

    [CreateAssetMenu(fileName = "CharacterType", menuName = "Metamatic/Nature/Character Type", order = 1)]
    public class CharacterType: ScriptableObject {
        public CharacterType baseType;
        public string title = "Unknown character";
    }
}