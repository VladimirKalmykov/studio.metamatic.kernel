using System;

namespace Metamatic
{
    public class RepositoryInternalStore {
    
    }
    public class RepositoryInternalStore<T>: RepositoryInternalStore
    where T: struct
    {
        private readonly ObservableWritableValue<T> m_Value = new();
        
        public void Get(out T value)
        {
            value = m_Value.value;
        }
        
        public void Set(T value)
        {
            m_Value.SetValue(value);
        }
        
        public Action Subscribe(Action<T> callback)
        {
            return m_Value.Subscribe(callback);
        }
    }
}