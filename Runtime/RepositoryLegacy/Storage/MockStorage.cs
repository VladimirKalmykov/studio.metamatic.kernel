using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Metamatic
{
    public class MockStorage: StorageLegacy
    {
        public Dictionary<(Type, string), (object, StorageLegacy.Metadata)> memory = new ();
        /// <summary>
        /// Provide fake latency for the storage
        /// </summary>
        public float latency = 0f;
        [CanBeNull] public Exception getException;
        [CanBeNull] public Exception setException;
        [CanBeNull] public Exception deleteException;
        public event Action<Type, Type, string> OnGetRequest;
        public event Action<Type, Type, string> OnSetRequest;
        public event Action<Type, string> OnDeleteRequest;
        public override async UniTask<(T, Metadata, TMetadata)> Get<T, TMetadata>(string address, CancellationToken token)
        {
            OnGetRequest?.Invoke(typeof(T), typeof(TMetadata), address);
            if (!enabled)
            {
                throw new StorageLegacy.DisabledException();
            }

            if (latency > 0f)
            {
                /* Wait for the latency */
                await Delay(TimeSpan.FromSeconds(latency), token);
            }
            
            if (getException != null)
            {
                throw getException;
            }
            
            var taskCompletionSource = new UniTaskCompletionSource<(T, Metadata, TMetadata)>();
            var type = typeof(T);
            if (memory.TryGetValue((type, address), out var value))
            {
                var metadataWithCustom = (Metadata<TMetadata>)value.Item2;
                var customMetadata = metadataWithCustom.custom;
                taskCompletionSource.TrySetResult(((T)value.Item1, (Metadata)value.Item2, customMetadata));
            }
            else
            {
                taskCompletionSource.TrySetException(new StorageLegacy.NotFoundException("Key not found"));
            }

            return await taskCompletionSource.Task;
        }

        public async override UniTask Set<T, TMetadata>(string address, T value, TMetadata customMetadata, CancellationToken token)
        {
            OnSetRequest?.Invoke(typeof(T), typeof(TMetadata), address);
            if (!enabled)
            {
                throw new StorageLegacy.DisabledException();
            }
            
            if (latency > 0f)
            {
                /* Wait for the latency */
                await Delay(TimeSpan.FromSeconds(latency), token);
            }
            
            if (setException != null)
            {
                throw setException;
            }
            
            var taskCompletionSource = new UniTaskCompletionSource();
            var type = typeof(T);
            var metadata = SerializableDataPackageLegacy<T, TMetadata>.GetMetadata(value, customMetadata);
            
            memory[(type, address)] = (value, metadata);
            taskCompletionSource.TrySetResult();
            await taskCompletionSource.Task;
        }

        public async override UniTask Delete<TData>(string address, CancellationToken token)
        {
            OnDeleteRequest?.Invoke(typeof(TData), address);
            if (!enabled)
            {
                throw new StorageLegacy.DisabledException();
            }
            
            if (latency > 0f)
            {
                /* Wait for the latency */
                await Delay(TimeSpan.FromSeconds(latency), token);
            }
            
            if (deleteException != null)
            {
                throw deleteException;
            }
            
            var taskCompletionSource = new UniTaskCompletionSource();
            var type = typeof(TData);
            if (memory.Remove((type, address)))
            {
                taskCompletionSource.TrySetResult();
            }
            else
            {
                taskCompletionSource.TrySetException(new StorageLegacy.NotFoundException("Key not found"));
            }

            await taskCompletionSource.Task;
        }
        
        private async UniTask Delay(TimeSpan delay, CancellationToken cancellationToken)
        {
            
#if UNITY_EDITOR
                var delayMilliseconds = delay.TotalMilliseconds;
                var startTime = EditorApplication.timeSinceStartup;

                while (EditorApplication.timeSinceStartup - startTime < delayMilliseconds / 1000)
                {
                    if (cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException();

                    await UniTask.Yield(PlayerLoopTiming.Update);
                }
#else
            await UniTask.Delay(delay, cancellationToken: cancellationToken);
#endif
        }
    }
}