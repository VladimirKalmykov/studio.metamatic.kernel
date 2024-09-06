using System;
using Object = UnityEngine.Object;

namespace Metamatic.Effects
{
    public interface IEffect
    {
        public Object producer { get; }
        public Object consumer { get; }
        public float startTime { get; }
        public Type type { get; }
        public bool instant { get; }
        public CancellationSignal cancellationSignal { get; }
        public DisposableAction SubscribeToRelease(Action action);
    }
}