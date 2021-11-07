using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsyncPipe<T> where T : struct
{
    public struct CancellationToken
    {
        public System.Threading.CancellationToken nativeToken;

        // ReSharper disable once InconsistentNaming
        public bool IsCancellationRequested
        {
            get
            {
                return nativeToken.IsCancellationRequested;
            }
        }

        public CancellationToken(System.Threading.CancellationToken nativeToken)
        {
            this.nativeToken = nativeToken;
        }
    }
    public delegate void PromiseFunc(Action<T> resolve, Action<Exception> reject);
    public delegate PromiseFunc Modifier(T value, CancellationToken cancellationToken);
    public readonly List<Modifier> modifiers = new List<Modifier>();
    public readonly List<CancellationTokenSource> processes = new List<CancellationTokenSource>();

    public async Task<T> Invoke(T value = default, System.Threading.CancellationToken cancellationToken = default)
    {
        var cancellationTokenSource = new CancellationTokenSource();
        CancellationTokenRegistration cancellationTokenRegistration;
        cancellationTokenRegistration = cancellationToken.Register(() =>
        {
            cancellationTokenRegistration.Dispose();
            if (!cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
            }
        });
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
                value = await AsPromise(queueCopy[i](value, new CancellationToken(cancellationTokenSource.Token)),
                    cancellationTokenSource.Token);
            }
            catch (OperationCanceledException e)
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

    private static Task<T> AsPromise(PromiseFunc func, System.Threading.CancellationToken cancellationToken)
    {
        var taskCompletionSource = new TaskCompletionSource<T>();
        CancellationTokenRegistration cancellationTokenRegistration;
        cancellationTokenRegistration = cancellationToken.Register(() =>
        {
            cancellationTokenRegistration.Dispose();
            taskCompletionSource.TrySetCanceled();;
        });
        Action<T> resolve = (T value) =>
        {
            if (cancellationToken.IsCancellationRequested) return;
            taskCompletionSource.TrySetResult(value);
        };
        Action<Exception> reject = (Exception exception) =>
        {
            if (cancellationToken.IsCancellationRequested) return;
            taskCompletionSource.TrySetException(exception);
        };
        func(resolve, reject);
        return taskCompletionSource.Task;
    }

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
