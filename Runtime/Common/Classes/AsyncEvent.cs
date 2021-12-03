using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public class AsyncEvent: IDisposable
    {
        public readonly struct CancellationEventMessage {}
        private List<Action<Payload>> listeners = new List<Action<Payload>>();
        private TaskCompletionSource<bool> m_EventHandlingProcessed = null;
        public async void Emit<T>(T value = default)
        {
            while (m_EventHandlingProcessed != null)
            {
                await m_EventHandlingProcessed.Task;
            }
            m_EventHandlingProcessed = new TaskCompletionSource<bool>();
            var payload = new Payload<T>(value);
            foreach (var listener in listeners.ToArray())
            {
                listener(payload);
            }

            var pendingTask = m_EventHandlingProcessed;
            m_EventHandlingProcessed = null;
            pendingTask.SetResult(true);
        }

        public async Task<Payload> WhenEmit(CancellationToken cancellationToken = default)
        {
            var taskCompletionSource = new TaskCompletionSource<Payload>();

            Action<Payload> listener = null;
            listener = (Payload payload) =>
            {
                listeners.Remove(listener);
                taskCompletionSource.SetResult(payload);
            };
            listeners.Add(listener);

            var waitCancelTask = new Promise<Payload>((resolve, reject, self) =>
            {
                var registration = cancellationToken.Register(() =>
                {
                    listeners.Remove(listener);
                    resolve(new Payload<CancellationEventMessage>());
                });
                self.OnFinally += () => { registration.Dispose(); };
            }).Task;

            var resultTask = await Task.WhenAny(new Task<Payload>[] {
                    taskCompletionSource.Task,
                    waitCancelTask
            });
            var result = await resultTask;
            return result switch
            {
                Payload<CancellationEventMessage> ct => throw new OperationCanceledException(),
                _ => result
            };
        }
        
        public void Dispose()
        {
            var boiloutPayload = new Payload<CancellationEventMessage>();
            foreach (var listener in listeners.ToArray())
            {
                listener(boiloutPayload);
            }

            listeners.Clear();
            listeners = null;
        }
    }
}