using Cysharp.Threading.Tasks;
using Metamatic;
using UnityEngine;

namespace Extensions
{
    public static class ParticleSystemExtensions
    {
        public static UniTask WaitUntilStopped(this ParticleSystem particleSystem, CancellationSignal signal)
        {
            return UniTask.WaitWhile(() => particleSystem.IsAlive() || signal.IsCancellationRequested);
        }
    }
}