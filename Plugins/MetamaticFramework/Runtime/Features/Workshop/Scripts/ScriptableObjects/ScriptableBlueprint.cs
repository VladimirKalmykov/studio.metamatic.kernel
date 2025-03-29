using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Metamatic.Systems;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace Metamatic.Framework.Workshop
{
    public abstract class ScriptableBlueprint : ScriptableObject, IObjectBlueprint
    {
        private static readonly List<ScriptableBlueprint> s_Blueprints = new List<ScriptableBlueprint>();
        public abstract string blueprintName { get; }
        [SerializeField] protected AssetReferenceT<GameObject> prefab;
        public abstract Type type { get; }

        public AsyncSummon<GameObject> SummonPrefab()
        {
            return ResourcesSystem.Summon(prefab);
        }
        
        public AsyncSummon<TOutput> SummonPrefabConverted<TOutput>(Func<GameObject, TOutput> converter)
        {
            return ResourcesSystem.SummonConverted<GameObject, TOutput>(prefab, converter);
        }
        
        private void OnEnable()
        {
            s_Blueprints.Add(this);
        }
        
        private void OnDisable()
        {
            s_Blueprints.Remove(this);
        }
    }
    public abstract class ScriptableBlueprint<T>: ScriptableBlueprint
    where T: Component
    {
        public abstract UniTask<string> GetDisplayName(Signal cancellationSignal = default);

        public abstract UniTask<Texture2D> GetPreview(Signal cancellationSignal = default);
        public override Type type => typeof(T);
        
        public AsyncSummon<T> SummonPrefabT()
        {
            return ResourcesSystem.SummonConverted<GameObject, T>(prefab, p => p.GetComponent<T>());
        }
    }
    
}