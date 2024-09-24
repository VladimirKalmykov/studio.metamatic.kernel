using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Metamatic.Framework.Workshop
{
    public abstract class ScriptableBlueprint : ScriptableObject, IObjectBlueprint
    {
        public abstract string blueprintName { get; }
        public abstract Type type { get; }
    }
    public abstract class ScriptableBlueprint<T>: ScriptableBlueprint
    where T: Component
    {
        public abstract UniTask<T> GetPrefab(CancellationSignal cancellationSignal = default);

        public abstract UniTask<T> Spawn(Vector3 position = default, Quaternion rotation = default,
            CancellationSignal cancellationSignal = default);

        public abstract void ReturnObject(T gameObject);

        public abstract UniTask<string> GetDisplayName(CancellationSignal cancellationSignal = default);

        public abstract UniTask<Texture2D> GetPreview(CancellationSignal cancellationSignal = default);
        public override Type type => typeof(T);
    }
}