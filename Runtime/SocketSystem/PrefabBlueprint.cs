using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Metamatic.SocketSystem
{
    [CreateAssetMenu(fileName = "PrefabBlueprint", menuName = "SpaceForge2025/Blueprint/Prefab", order = 0)]
    public class PrefabBlueprint: ScriptableBlueprint
    {
        public override Type type => typeof(GameObject);
    }
}