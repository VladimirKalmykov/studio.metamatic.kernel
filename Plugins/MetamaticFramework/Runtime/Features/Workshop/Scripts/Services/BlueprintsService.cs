using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Framework.Workshop
{
    public class BlueprintsService: Service
    {
        [SerializeField] private ScriptableBlueprint[] m_Blueprints;
        
        private Dictionary<string, IObjectBlueprint> m_BlueprintMap = new ();
        protected override async UniTask RunService(Ready ready, Signal signal)
        {
            foreach (var blueprint in m_Blueprints)
            {
                #if DEBUG
                if (m_BlueprintMap.ContainsKey(blueprint.blueprintName))
                {
                    Console.Exception("SpawnService", $"Blueprint with name {blueprint.blueprintName} already exists.");
                }
                #endif
                m_BlueprintMap[blueprint.blueprintName] = blueprint;
            }
            await ready();
        }
        
        public static async UniTask<(T, IObjectBlueprint)> Spawn<T>(IObjectBlueprint<T> blueprint, Vector3 position = default, Quaternion rotation = default, Signal cancellationSignal = default) where T: Behaviour
        {
            var instance = await blueprint.ProduceObject(cancellationSignal: cancellationSignal);
            var origin = instance.gameObject.TryAddComponent<BlueprintOrigin>();
            origin.blueprint = blueprint;
            return (instance, blueprint);
        }
        
        public static void Return<T>(T instance, IObjectBlueprint<T> blueprint) where T: Behaviour
        {
            blueprint.ReturnObject(instance);
        }
    }
}