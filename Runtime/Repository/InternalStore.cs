using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public abstract class InternalStore
    {
        public enum Status
        {
            Idle,
            Fetching,
            Pushing
        }
        public enum DataOrigin
        {
            Uninitialized,
            Persistent,
            Local,
            Default
        }
        public struct DataStatus
        {
            public Status storeStatus;
            public DataOrigin origin;
            public bool isDirty;
        }
        public readonly string uri;
        public readonly Type type;
        public InternalStore(Type type, string uri)
        {
            this.type = type;
            this.uri = uri;
        }
    }
    
    public class InternalStore<T>: InternalStore
    where T: struct
    {
        private T defaultValue;
        /// <summary>
        /// Current value of the store.
        /// </summary>
        private T m_Value;
        public T value
        {
            get => m_Value;
            set
            {
                SetValue(value);
            }
        }
        /// <summary>
        /// Initial value of the store (used to reset the value).
        /// </summary>
        public T initialValue;
        /// <summary>
        /// Determines the origin of the data (concrete data which is stored in the originalValue property).
        /// </summary>
        public DataOrigin dataOrigin { get; private set; } = DataOrigin.Uninitialized;

        public bool isDirty { get; private set; } = false;
        public readonly Repository repository;
        public bool autoPush;
        private ObservableWritableValue<Status> m_Status = new ObservableWritableValue<Status>(Status.Idle);
        public Status status
        {
            get => m_Status.value;
            private set => m_Status.value = value;
        }
        private Exception m_Exception;
        private int m_AcquireCount;
        public event Action<Exception> OnError;
        /// <summary>
        /// Event is triggered when the store is synced with the repository source.
        /// </summary>
        public event Action<Exception> OnSyncFailed;
        public event Action<T> OnValueChange;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="repository">Ref to repository</param>
        /// <param name="uri">State uri</param>
        /// <param name="autoPush">Auto push to repository source on changes</param>
        public InternalStore(Repository repository, string uri, bool autoPush = false, T defaultValue = default): base(typeof(T), uri) {
            m_Value = defaultValue;
            this.repository = repository;
            this.autoPush = autoPush;
            this.defaultValue = defaultValue;
        }
        
        /// <summary>
        /// Wait until the store is synced with the repository source.
        /// </summary>
        public async UniTask WhenSynced(CancellationToken token = default)
        {
            await m_Status.WaitUntilValueIs(Status.Idle, token);
        }
        
        /// <summary>
        /// Fetch the value from the repository source.
        /// </summary>
        public async void Fetch(CancellationToken token = default)
        {
            if (status == Status.Fetching)
            {
                await m_Status.WaitUntilValueIsNot(Status.Idle, token);
                return;
            } else if (status != Status.Idle)
            {
                await m_Status.WaitUntilValueIs(Status.Idle, token);
            }
            status = Status.Fetching;
            try
            {
                var newValue = await repository.Read<T>(Repository.GetUriAddress<T>(uri), repository.source, token);
                initialValue = newValue;
                dataOrigin = DataOrigin.Persistent;
                this.value = newValue;
            }
            catch (OperationCanceledException)
            {
                // Do nothing
            }
            catch (DirectoryNotFoundException)
            {
                dataOrigin = DataOrigin.Default;
                OnSyncFailed?.Invoke(new DirectoryNotFoundException($"Directory not found for {uri}"));
            }
            catch (FileNotFoundException)
            {
                dataOrigin = DataOrigin.Default;
                OnSyncFailed?.Invoke(new FileNotFoundException($"File not found for {uri}"));
            }
            catch (System.Exception e)
            {
                dataOrigin = DataOrigin.Default;
                AssertError(new ReadRepositoryException(uri: uri, innerException: e, message: $"Internal store fetch error: {e.Message}"));
            }
            finally
            {
                status = Status.Idle;
            }
        }
        
        /// <summary>
        /// Push the value to the repository source.
        /// </summary>
        /// <param name="token"></param>
        public async UniTask Push(CancellationToken token = default)
        {
            if (status != Status.Idle)
            {
                await m_Status.WaitUntilValueIs(Status.Idle, token);
            }
            status = Status.Pushing;
            try
            {
                await repository.Write<T>(Repository.GetUriAddress<T>(uri), value, repository.source, token);
                dataOrigin = DataOrigin.Persistent;
                initialValue = value;
                isDirty = false;
            }
            catch (System.Exception e)
            {
                AssertError(new WriteRepositoryException(uri: uri, innerException: e, message: "Internal store push error: " + e.Message));
                throw e;
            }
            finally
            {
                status = Status.Idle;
            }
        }
        
        /// <summary>
        /// Asquire the store. Should be called before using the store. (Do not call this method directly)
        /// </summary>
        public void Asquire()
        {
            m_AcquireCount++;
            if (m_AcquireCount == 1)
            {
                Fetch();
            }
        }
        
        /// <summary>
        /// Release the store. Should be called after using the store. (Do not call this method directly)
        /// </summary>
        public void Release()
        {
            m_AcquireCount--;
            if (m_AcquireCount <= 0)
            {
                repository.Remove(this);
            }
        }
        
        public void Reset()
        {
            value = initialValue;
            isDirty = false;
        }
        
        public void SetValue(T value)
        {
            this.m_Value = value;
            isDirty = true;
            dataOrigin = DataOrigin.Local;
            OnValueChange?.Invoke(value);
            if (autoPush)
            {
                Push();
            }
        }
        
        public Action Subscribe(Action<T> action)
        {
            var subscriptionCancellationTokenSource = new CancellationTokenSource();
            /* Standart subscription should provide value immediately. But we should wait util the store is synchronized */
            WhenSynced(subscriptionCancellationTokenSource.Token).ContinueWith(() =>
            {
                action(value);
            }).Forget();
            OnValueChange += action;
            return () =>
            {
                subscriptionCancellationTokenSource.Cancel();
                OnValueChange -= action;
            };
        }
        
        /// <summary>
        /// Get current store status as a DataStatus object.
        /// </summary>
        /// <returns></returns>
        public DataStatus GetDataStatus()
        {
            return new DataStatus()
            {
                storeStatus = status,
                origin = dataOrigin,
                isDirty = isDirty
            };
        }

        private void AssertError(Exception ex)
        {
            /* Tell repository that an error occurred */
            repository.AssertError(ex);
            OnError?.Invoke(ex);
            m_Exception = ex;
        }
    }
}