namespace Metamatic
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    public class Pipe<T>
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
        public delegate Task<T> Modifier(T value, CancellationToken cancellationToken);
        public readonly List<Modifier> modifiers = new List<Modifier>();
        public readonly List<CancellationTokenSource> processes = new List<CancellationTokenSource>();

        // public Task<Payload> Invoke<T>(T data, System.Threading.CancellationToken cancellationToken = default) where T: notnull
        // {
        //     return Invoke(new Payload<T>(data), cancellationToken);
        // }
        public async Task<T> Invoke(T value = default, System.Threading.CancellationToken cancellationToken = default)
        {
            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            processes.Add(cancellationTokenSource);
            var queueCopy = modifiers.ToArray();
            for (var i = 0; i < queueCopy.Length; i++)
            {
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    cancellationTokenSource.Dispose();
                    processes.Remove(cancellationTokenSource);
                    return value;
                }

                try
                {
                    value = await queueCopy[i](value, cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    cancellationTokenSource.Dispose();
                    processes.Remove(cancellationTokenSource);
                    return value;
                }
            }
            
            cancellationTokenSource.Dispose();
            processes.Remove(cancellationTokenSource);
            return value;
        }

        public void Attach(Modifier modifier)
        {
            modifiers.Add(modifier);
        }

        public void Detach(Modifier modifier)
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