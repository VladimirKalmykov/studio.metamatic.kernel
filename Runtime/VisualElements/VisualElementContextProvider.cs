using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Metamatic
{
    public class VisualElementContextProvider: VisualElement
    {
        private readonly Dictionary<Type, object> m_Contexts = new Dictionary<Type, object>();
        public VisualElementContextProvider()
        {
            
        }

        public void AddContext<T>(Observable<T> contextSource)
        {
            m_Contexts.Add(typeof(T), contextSource);   
        }
        
        public ObservableReadOnly<T> GetContext<T>()
        {
            if (m_Contexts.TryGetValue(typeof(T), out var context))
            {
                return (ObservableReadOnly<T>) context;
            }
            return null;
        }
    }
}