namespace Metamatic
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    public class AsyncPipe
    {
        // public struct CancellationToken
        // {
        //     public System.Threading.CancellationToken nativeToken;
        //
        //     // ReSharper disable once InconsistentNaming
        //     public bool IsCancellationRequested
        //     {
        //         get
        //         {
        //             return nativeToken.IsCancellationRequested;
        //         }
        //     }
        //
        //     public CancellationToken(System.Threading.CancellationToken nativeToken)
        //     {
        //         this.nativeToken = nativeToken;
        //     }
        // }
        // public delegate void PromiseFunc(Action<Payload> resolve, Action<Exception> reject);
        public delegate Task<Payload> Modifier(Payload value, CancellationToken cancellationToken);
        public readonly List<Modifier> modifiers = new List<Modifier>();
        public readonly List<CancellationTokenSource> processes = new List<CancellationTokenSource>();

        // public Task<Payload> Invoke<T>(T data, System.Threading.CancellationToken cancellationToken = default) where T: notnull
        // {
        //     return Invoke(new Payload<T>(data), cancellationToken);
        // }
        public async Task<Payload> Invoke<T>(T value = default, System.Threading.CancellationToken cancellationToken = default)
        {
            var payload = (Payload)(new Payload<T>(value));
            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            processes.Add(cancellationTokenSource);
            var queueCopy = modifiers.ToArray();
            for (var i = 0; i < queueCopy.Length; i++)
            {
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    cancellationTokenSource.Dispose();
                    processes.Remove(cancellationTokenSource);
                    return payload;
                }

                try
                {
                    payload = await queueCopy[i](payload, cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    cancellationTokenSource.Dispose();
                    processes.Remove(cancellationTokenSource);
                    return payload;
                }
            }
            
            cancellationTokenSource.Dispose();
            processes.Remove(cancellationTokenSource);
            return payload;
        }

        // private static Task<Payload> AsPromise(PromiseFunc func, System.Threading.CancellationToken cancellationToken)
        // {
        //     var taskCompletionSource = new TaskCompletionSource<Payload>();
        //     CancellationTokenRegistration cancellationTokenRegistration;
        //     cancellationTokenRegistration = cancellationToken.Register(() =>
        //     {
        //         // ReSharper disable once AccessToModifiedClosure
        //         cancellationTokenRegistration.Dispose();
        //         taskCompletionSource.TrySetCanceled();;
        //     });
        //     Action<Payload> resolve = (Payload value) =>
        //     {
        //         if (cancellationToken.IsCancellationRequested) return;
        //         taskCompletionSource.TrySetResult(value);
        //     };
        //     Action<Exception> reject = (Exception exception) =>
        //     {
        //         if (cancellationToken.IsCancellationRequested) return;
        //         taskCompletionSource.TrySetException(exception);
        //     };
        //     func(resolve, reject);
        //     return taskCompletionSource.Task;
        // }

        public void Pipe(Modifier modifier)
        {
            modifiers.Add(modifier);
        }

        public void Unpipe(Modifier modifier)
        {
            modifiers.Remove(modifier);
        }

        public void Dispose()
        {
            foreach (var cancellationTokenSource in processes)
            {
                cancellationTokenSource.Cancel();
            }
        }
    }

}