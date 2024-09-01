using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.UIElements;

namespace Metamatic.EditorInspector
{
    public abstract class InspectorPanelVisualElement: VisualElement
    {
        public InspectorPanelVisualElement(CancellationToken cancellationToken)
        {
            OnPanelStart(cancellationToken).Forget();
        }

        protected abstract UniTaskVoid OnPanelStart(CancellationToken cancellationToken);
    }
}