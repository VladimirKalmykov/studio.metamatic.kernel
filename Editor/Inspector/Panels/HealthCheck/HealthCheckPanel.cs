using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.EditorInspector
{
    public class HealthCheckPanel: InspectorPanelVisualElement
    {
        public HealthCheckPanel(CancellationToken cancellationToken) : base(cancellationToken)
        {
        }

        protected override UniTaskVoid OnPanelStart(CancellationToken cancellationToken)
        {
            // Здесь можно инициализировать элемент
            return new UniTaskVoid();
        }
    }
}