using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.UIElements;

namespace Metamatic.EditorTools
{
    public abstract class AsyncVisualElementContextProvider<T>: AsyncVisualElement
    {
        private readonly Observable<T> m_Context;
        public ObservableReadOnly<T> context => m_Context;
        public AsyncVisualElementContextProvider(Observable<T> context, CancellationSignal signal): base(signal)
        {
            m_Context = context;
        }
        
        public ObservableReadOnly<T> GetContext()
        {
            if (context == null)
            {
                /* If context is not found then search for parent context */
                var currentElement = parent;
                while (currentElement != null)
                {
                    if (currentElement is AsyncVisualElementContextProvider<T> asyncVisualElementContextProvider)
                    {
                        return asyncVisualElementContextProvider.GetContext();
                    }
                    currentElement = currentElement.parent;
                }
                return null;
            }
            return context;
        }
    }

    public static class VisualElementExtensions
    {
        public static ObservableReadOnly<TContext> GetContext<TContext>(this VisualElement visualElement)
        {
            var currentElement = visualElement;
            while (currentElement != null)
            {
                if (currentElement is AsyncVisualElementContextProvider<TContext> asyncVisualElementContextProvider)
                {
                    return asyncVisualElementContextProvider.GetContext();
                }
                currentElement = currentElement.parent;
            }
            return null;
        }
    }
}