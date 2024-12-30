using System;
using Cysharp.Threading.Tasks;
using Metamatic;
using UnityEngine;

namespace Extensions
{
    public static class ParticleSystemExtensions
    {
        public static UniTask WaitUntilStopped(this ParticleSystem particleSystem, Signal signal = default)
        {
            return UniTask.WaitWhile(() => particleSystem != null && particleSystem.IsAlive() && !signal.IsCancellationRequested);
        }
        
        public static async UniTask WaitUntilStopped(this ParticleSystem particleSystem, Action onFrame, Signal signal = default, PlayerLoopTiming timing = PlayerLoopTiming.Update)
        {
            await UniTask.WaitWhile(() =>
            {
                onFrame();
                return particleSystem != null && particleSystem.IsAlive() && !signal.IsCancellationRequested;
            }, timing: timing);
            signal.ThrowIfRaised();
        }
    }
}