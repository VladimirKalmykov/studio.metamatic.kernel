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
        public UniTask<string> GetDisplayName(Signal cancellationSignal = default);
        /// <summary>
        /// Get the preview of the object.
        /// </summary>
        /// <returns></returns>
        public UniTask<Texture2D> GetPreview(Signal cancellationSignal = default);
    }
}