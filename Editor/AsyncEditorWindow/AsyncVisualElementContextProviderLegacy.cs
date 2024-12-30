using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.UIElements;

namespace Metamatic.EditorTools
{
    public abstract class AsyncVisualElementContextProviderLegacy<T>: AsyncVisualElementLegacy
    {
        private readonly Observable<T> m_Context;
        public ObservableReadOnly<T> context => m_Context;
        public AsyncVisualElementContextProviderLegacy(Observable<T> context, Signal signal): base(signal)
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
                    if (currentElement is AsyncVisualElementContextProviderLegacy<T> asyncVisualElementContextProvider)
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

    public static class VisualElementExtensionsLegacy
    {
        public static ObservableReadOnly<TContext> GetContextLegacy<TContext>(this VisualElement visualElement)
        {
            var currentElement = visualElement;
            while (currentElement != null)
            {
                if (currentElement is AsyncVisualElementContextProviderLegacy<TContext> asyncVisualElementContextProvider)
                {
                    return asyncVisualElementContextProvider.GetContext();
                }
                currentElement = currentElement.parent;
            }
            return null;
        }
    }
}