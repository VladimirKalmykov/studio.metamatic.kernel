using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public class ProceduralAnimationClip<TSignal>: MicroBehaviour
    {
        public delegate void SignalHandler(float timestamp);
        private Dictionary<TSignal, (bool was, float timestamp, Queue<SignalHandler> queue)> m_Signals = new ();
        private float m_StartTime;
        public ProceduralAnimationClip(CancellationToken cancellationToken)
        {
            Run(cancellationToken);
        }
        protected override async UniTask Main(CancellationToken cancellationToken)
        {
            m_StartTime = Time.time;
            await cancellationToken;
        }
        
        public void Signal(TSignal signalName)
        {
            if (!m_Signals.TryGetValue(signalName, out var queue))
            {
                queue = (true, Time.time, new Queue<SignalHandler>());
                m_Signals.Add(signalName, queue);
            }
            
            queue.was = true;
            queue.timestamp = Time.time;
            
            if (queue.queue.Count > 0)
            {
                queue.queue.Dequeue().Invoke(queue.timestamp - m_StartTime);
            }
        }
        
        public DisposableAction SubscribeSignal(TSignal signalName, SignalHandler action, bool waitForNew = false)
        {
            if (!m_Signals.TryGetValue(signalName, out var queue))
            {
                queue = (false, 0f, new Queue<SignalHandler>());
                m_Signals.Add(signalName, queue);
            } else if (queue.was && !waitForNew)
            {
                action.Invoke(queue.timestamp - m_StartTime);
            }
            queue.queue.Enqueue(action);
            return new DisposableAction(() =>
            {
                queue.queue.Dequeue();
            });
        }
        
        public UniTask<float> WhenSignal(TSignal signalName, CancellationToken cancellationToken, bool waitForNew = false)
        {
            var taskCompletionSource = new UniTaskCompletionSource<float>();
            var localCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.cancellationToken);
            CancellationTokenRegistration registration = default;
            var resolved = false;
            var disposable = SubscribeSignal(signalName, timestamp =>
            {
                resolved = true;
                localCancellationTokenSource.Dispose();
                registration.Dispose();
                taskCompletionSource.TrySetResult(timestamp);
            }, waitForNew);
            if (!resolved)
            {
                registration = localCancellationTokenSource.Token.Register(() =>
                {
                    localCancellationTokenSource.Dispose();
                    taskCompletionSource.TrySetCanceled();
                    disposable.Dispose();
                });
            }
            return taskCompletionSource.Task;
        }
    }
}