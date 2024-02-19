using System;

namespace Metamatic
{
    public class InternalStore
    {
        
    }
    
    public class InternalStore<T>: InternalStore
    {
        public enum Status
        {
            Idle,
            Reading,
            Writing,
            Error
        }
        public T value;
        public readonly string uri;
        public readonly Repository repository;
        private ObservableWritableValue<Status> m_Status = new ObservableWritableValue<Status>(Status.Idle);
        public Status status
        {
            get => m_Status.value;
            private set => m_Status.value = value;
        }
        private Exception m_Exception;
        public InternalStore(Repository repository, string uri) {
            this.repository = repository;
            this.uri = uri;
        }
        
        /// <summary>
        /// Fetch the value from the source.
        /// </summary>
        public async void Fetch()
        {
            status = Status.Reading;
            try
            {
                this.value = await repository.Read<T>(uri, repository.source);
                status = Status.Idle;
            }
            catch (System.Exception e)
            {
                status = Status.Error;
                m_Exception = e;
            }
        }
    }
}