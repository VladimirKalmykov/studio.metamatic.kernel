namespace Metamatic {
    using UnityEngine;
    using Metamatic;
    
    [CreateAssetMenu(fileName = "Stock", menuName = "Dark Matter/Stock", order = 4)]
    public class Stock: ScriptableSet<GameObject> {
        public new string name = "Universal";
    }
}