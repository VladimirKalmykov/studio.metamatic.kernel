using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Framework.Workshop
{
    public abstract class ScriptableBlueprint : ScriptableObject, IObjectBlueprint
    {
        public abstract string blueprintName { get; }
        public abstract Type type { get; }
        public abstract UniTask<GameObject> GetPrefab(Signal cancellationSignal = default);
        public abstract UniTask<GameObject> Spawn(Vector3 position = default, Quaternion rotation = default,
            Signal cancellationSignal = default);
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