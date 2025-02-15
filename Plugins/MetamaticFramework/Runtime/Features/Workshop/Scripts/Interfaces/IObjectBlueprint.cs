using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Framework.Workshop
{
    public interface IObjectBlueprint
    {
        public string blueprintName { get; }
        public Type type { get; }
    }
    public interface IObjectBlueprint<T>: IObjectBlueprint
    where T: Component
    {
        public UniTask<T> GetPrefab(Signal cancellationSignal = default);
        public AsyncSummon<T> Summon(Signal cancellationSignal = default);
        /// <summary>
        /// Produces an object from the blueprint.
        /// </summary>
        /// <returns></returns>
        [Obsolete("Use Summon() instead.", true)]
        public UniTask<T> ProduceObject(Vector3 position = default, Quaternion rotation = default, Signal cancellationSignal = default);
        /// <summary>
        /// Returns the object to the blueprint (e.g. for pooling).
        /// </summary>
        /// <param name="gameObject"></param>
        [Obsolete("Use Summon() instead.", true)]
        public void ReturnObject(T gameObject);
        /// <summary>
        /// Gets the display name of the object.
        /// </summary>
        /// <returns></returns>
        public UniTask<string> GetDisplayName(Signal cancellationSignal = default);
        /// <summary>
        /// Get the preview of the object.
        /// </summary>
        /// <returns></returns>
        public UniTask<Texture2D> GetPreview(Signal cancellationSignal = default);
    }
}