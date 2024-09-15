using Cysharp.Threading.Tasks;
using Metamatic;
using UnityEngine;

namespace Extensions
{
    public static class ParticleSystemExtensions
    {
        public static UniTask WaitUntilStopped(this ParticleSystem particleSystem, CancellationSignal signal = default)
        {
            return UniTask.WaitWhile(() => particleSystem != null && particleSystem.IsAlive() && !signal.IsCancellationRequested);
        }
    }
}