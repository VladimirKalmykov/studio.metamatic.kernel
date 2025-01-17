using UnityEngine.UIElements;

namespace Metamatic
{
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
        
        public static void SetPickingModeRecursively(this VisualElement element, PickingMode mode)
        {
            element.pickingMode = mode;
            for (int i = 0; i < element.hierarchy.childCount; i++)
                element.hierarchy.ElementAt(i).SetPickingModeRecursively(mode);
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