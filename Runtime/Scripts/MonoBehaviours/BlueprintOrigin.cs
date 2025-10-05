using Metamatic.SocketSystem;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metamatic
{
    public class BlueprintOrigin: MonoBehaviour
    {
        [ShowInInspector, ReadOnly]
        public IObjectBlueprint blueprint { get; set; }
        [ShowInInspector, ReadOnly]
        public string address { get; set; }
        
    }
}