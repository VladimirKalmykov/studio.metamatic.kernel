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
        private TaskCompletionSource<Payload> m_TaskCompletionSource = null;
        public void Emit<T>(T value = default)
        {
            var source = m_TaskCompletionSource;
            if (source == null) return;
            m_TaskCompletionSource = null;
            source.SetResult(new Payload<T>(value));
            source.Task.Dispose();
        }

        public async Task<Payload> WhenEmit(CancellationToken cancellationToken = default)
        {
            if (m_TaskCompletionSource == null)
            {
                m_TaskCompletionSource = new TaskCompletionSource<Payload>();
            }

            var waitCancelTask = new Promise<Payload>((resolve, reject, self) =>
            {
                var registration = cancellationToken.Register(() => resolve(new Payload<CancellationEventMessage>()));
                self.OnFinally += () => { registration.Dispose(); };
            }).Task;

            var resultTask = await Task.WhenAny(new Task<Payload>[] {
                    m_TaskCompletionSource.Task,
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
            m_TaskCompletionSource?.Task.Dispose();
        }
    }
}