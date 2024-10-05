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
    
    public static class VisualElementExtensions
    {
        public static ObservableReadOnly<TContext> GetContext<TContext>(this VisualElement visualElement)
        {
            var currentElement = visualElement;
            while (currentElement != null)
            {
                if (currentElement is VisualElementContextProvider visualElementContextProvider)
                {
                    return visualElementContextProvider.GetContext<TContext>();
                }
                currentElement = currentElement.parent;
            }
            return null;
        }
        
        public static T FindParent<T>(this VisualElement visualElement) where T: VisualElement
        {
            var currentElement = visualElement;
            while (currentElement != null)
            {
                if (currentElement is T t)
                {
                    return t;
                }
                currentElement = currentElement.parent;
            }
            return null;
        }
    }
}