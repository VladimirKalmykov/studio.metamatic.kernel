using System;
using System.Threading;
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
        public readonly string uri;
        public readonly Type type;
        public InternalStore(Type type, string uri)
        {
            this.type = type;
            this.uri = uri;
        }
    }
    
    public class InternalStore<T>: InternalStore
    {
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
        public readonly string uri;
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
        public event Action<T> OnValueChange;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="repository">Ref to repository</param>
        /// <param name="uri">State uri</param>
        /// <param name="autoPush">Auto push to repository source on changes</param>
        public InternalStore(Repository repository, string uri, bool autoPush = false): base(typeof(T), uri) {
            this.repository = repository;
            this.autoPush = autoPush;
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
                this.value = newValue;
            }
            catch (System.Exception e)
            {
                AssertError(e);
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
        public async void Push(CancellationToken token = default)
        {
            if (status != Status.Idle)
            {
                await m_Status.WaitUntilValueIs(Status.Idle, token);
            }
            status = Status.Pushing;
            try
            {
                await repository.Write<T>(Repository.GetUriAddress<T>(uri), value, repository.source, token);
            }
            catch (System.Exception e)
            {
                AssertError(e);
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
        }
        
        public void SetValue(T value)
        {
            this.value = value;
            OnValueChange?.Invoke(value);
            if (autoPush)
            {
                Push();
            }
        }
        
        public Action Subscribe(Action<T> action)
        {
            OnValueChange += action;
            return () => OnValueChange -= action;
        }

        private void AssertError(Exception ex)
        {
            Debug.LogError(ex);
            OnError?.Invoke(ex);
            m_Exception = ex;
        }
    }
}