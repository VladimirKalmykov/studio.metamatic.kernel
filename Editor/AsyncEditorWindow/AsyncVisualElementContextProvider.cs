using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.UIElements;

namespace Metamatic.EditorTools
{
    public abstract class AsyncVisualElementContextProvider<T>: AsyncVisualElement
    {
        private readonly MicroState<T> m_Context;
        public MicroStateReadOnly<T> context => m_Context;
        public AsyncVisualElementContextProvider(MicroState<T> context, CancellationToken cancellationToken): base(cancellationToken)
        {
            m_Context = context;
        }
        
        public MicroStateReadOnly<T> GetContext()
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
        public static MicroStateReadOnly<TContext> GetContext<TContext>(this VisualElement visualElement)
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