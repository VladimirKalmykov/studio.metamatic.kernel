using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metamatic.Framework.Workshop
{
    public abstract class ScriptableBlueprint : ScriptableObject, IObjectBlueprint
    {
        private static readonly List<ScriptableBlueprint> s_Blueprints = new List<ScriptableBlueprint>();
        [ValidateInput("ShouldBeUniqueWithinSystem", "$Message", InfoMessageType.Warning)]
        [SerializeField] private string address;
        public abstract string blueprintName { get; }
        public abstract Type type { get; }
        public abstract UniTask<GameObject> GetPrefab(Signal cancellationSignal = default);
        public abstract UniTask<GameObject> Spawn(Vector3 position = default, Quaternion rotation = default,
            Signal cancellationSignal = default);
        
        private void OnEnable()
        {
            s_Blueprints.Add(this);
        }
        
        private void OnDisable()
        {
            s_Blueprints.Remove(this);
        }

        [UsedImplicitly]
        private bool ShouldBeUniqueWithinSystem(string address)
        {
            return s_Blueprints.FindAll(x => x.address == address).Count <= 1;
        }
    }
    public abstract class ScriptableBlueprint<T>: ScriptableBlueprint
    where T: Component
    {
        public abstract UniTask<T> GetPrefabTyped(Signal cancellationSignal = default);

        public abstract AsyncSummon<T> Summon();

        public abstract UniTask<string> GetDisplayName(Signal cancellationSignal = default);

        public abstract UniTask<Texture2D> GetPreview(Signal cancellationSignal = default);
        public override Type type => typeof(T);
    }
    
}