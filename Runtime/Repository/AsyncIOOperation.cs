using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Metamatic
{
    public struct AsyncReadOperation<T>: INotifyCompletion {
        public AsyncIOOperationSource.Status status => m_Source.status.value;
        public float progress => m_Source.progress;
        public Exception exception => m_Source.exception;
        public bool isLoading => m_Source.isLoading;
        public bool isDone => m_Source.isDone;
        public void Cancel()
        {
            m_Source.Cancel();
        }

        private readonly AsyncReadOperationSource<T> m_Source;

        public AsyncReadOperation(AsyncReadOperationSource<T> source)
        {
            m_Source = source;
        }
        
        public bool IsCompleted => isDone;
        public void OnCompleted(Action continuation)
        {
            var operation = this;
            if (m_Source.status.value == AsyncIOOperationSource.Status.Success ||
                m_Source.status.value == AsyncIOOperationSource.Status.Error)
            {
                continuation();
            }
            else
            {
                Action subscription = null;
                subscription = m_Source.status.Subscribe((AsyncIOOperationSource.Status status) =>
                {
                    if (status == AsyncIOOperationSource.Status.Success ||
                        status == AsyncIOOperationSource.Status.Error)
                    {
                        subscription?.Invoke();
                        continuation();
                    }
                });
            }
        }

        public T GetResult()
        {
            if (m_Source.exception != null) throw m_Source.exception;
            return m_Source.result;
        }

        public AsyncReadOperation<T> GetAwaiter() => this;
    }
    public struct AsyncWriteOperation<T>: INotifyCompletion {
        public AsyncIOOperationSource.Status status => m_Source.status.value;
        public float progress => m_Source.progress;
        public Exception exception => m_Source.exception;
        public bool isLoading => m_Source.isLoading;
        public bool isDone => m_Source.isDone;
        public void Cancel()
        {
            m_Source.Cancel();
        }

        private readonly AsyncWriteOperationSource<T> m_Source;

        public AsyncWriteOperation(AsyncWriteOperationSource<T> source)
        {
            m_Source = source;
        }
        
        public bool IsCompleted => isDone;
        public void OnCompleted(Action continuation)
        {
            Action subscription = null;
            subscription = m_Source.status.Subscribe((AsyncIOOperationSource.Status status) =>
            {
                if (status == AsyncIOOperationSource.Status.Success ||
                    status == AsyncIOOperationSource.Status.Error)
                {
                    subscription?.Invoke();
                    continuation();
                }
            });
        }

        public void GetResult()
        {
            if (m_Source.exception != null) throw m_Source.exception;
        }

        public AsyncWriteOperation<T> GetAwaiter() => this;
    }

    public abstract class AsyncIOOperationSource
    {
        protected event Action OnCancel;
        public bool IsCancelled { get; private set; }
        public enum Status
        {
            Idle,
            Loading,
            Success,
            Error
        }
        public MicroState<Status> status = new MicroState<Status>(Status.Idle);
        public float progress { get; set; }
        public Exception exception { get; protected set; }
        public bool isLoading => status.value == Status.Loading;
        public bool isDone => status.value is Status.Success or Status.Error;

        public void Cancel()
        {
            IsCancelled = true;
            OnCancel?.Invoke();
        }
    }
    public abstract class AsyncIOOperationSource<T>: AsyncIOOperationSource
    {
        public void SetProgress(float progress)
        {
            this.progress = progress;
            if (status.value != Status.Loading)
            {
                status.value = Status.Loading;
            }
        }
        public void SetError(Exception exception)
        {
            this.progress = 1;
            this.exception = exception;
            status.value = Status.Error;
        }
    }
    public class AsyncReadOperationSource<T>: AsyncIOOperationSource<T>
    {
        public T? result { get; protected set; }
        public AsyncReadOperation<T> Operation => new AsyncReadOperation<T>(this);
        
        protected void SetResult(T? result)
        {
            this.progress = 1;
            this.result = result;
            status.value = Status.Success;
        }
        /// <summary>
        /// Set raw result from source and deserialize it with versioning logic.
        /// </summary>
        /// <param name="rawResult"></param>
        public void SetRawValue(string rawResult)
        {
            var testData = JsonUtility.FromJson<SerializableDataPackage<T>>(rawResult);
            
            if (testData.metadata.typeVersion != VersionAttribute.GetVersion<T>())
            {
                /* Migrate */
                var oldVersionType = VersionAttribute.GetTypeForSpecifiedVersion<T>(testData.metadata.typeVersion);
                Debug.Log($"Old version type is {oldVersionType}");
                Debug.Assert(oldVersionType != null, "Old version type is not supported");
                if (oldVersionType != null)
                {
                    Type genericType = typeof(SerializableDataPackage<>);
                    Type constructedType = genericType.MakeGenericType(oldVersionType);
                    var oldData = JsonUtility.FromJson(rawResult, constructedType);
                    var valueProperty = oldData.GetType().GetField("value");
                    if (valueProperty != null)
                    {
                        object value = valueProperty.GetValue(oldData);
                        
                        var migratedData = Migrate(testData.metadata.typeVersion, value);
                        SetResult(migratedData);
                    }
                }
                else
                {
                    SetResult(default);
                }
            }
            else
            {
                SetResult(testData.value);
            }
        }
        
        protected T Migrate(string version, object data)
        {
            var migrateMethod = typeof(T).GetMethod("Migrate", BindingFlags.Static | BindingFlags.Public);
            if (migrateMethod != null)
            {
                return (T)migrateMethod.Invoke(null, new object[] { version, data });
            }

            return default(T);
        }
    }
    public class AsyncWriteOperationSource<T>: AsyncIOOperationSource<T>
    {
        public AsyncWriteOperation<T> Operation => new AsyncWriteOperation<T>(this);
        public void SetCompleted()
        {
            this.progress = 1;
            status.value = Status.Success;
        }

        public string PrepareValue(T value)
        {
            var package = new SerializableDataPackage<T>(value);
            return JsonUtility.ToJson(package);
        }
    }
}