using Sirenix.OdinInspector;
using UnityEngine;

namespace Metamatic.Framework.Workshop
{
    public class BlueprintOrigin: MonoBehaviour
    {
        [ShowInInspector, ReadOnly]
        public IObjectBlueprint blueprint { get; set; }
        [ShowInInspector, ReadOnly]
        public string address { get; set; }
        
    }
}