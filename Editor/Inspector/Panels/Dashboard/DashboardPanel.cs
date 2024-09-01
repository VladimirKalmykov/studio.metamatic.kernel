using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic.EditorInspector
{
    public class DashboardPanel: InspectorPanelVisualElement
    {
        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            // Этот метод вызовется, когда элемент будет добавлен в панель
            Debug.Log("Tab1 attached to panel");
            // Здесь можно подписаться на глобальные события
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            // Этот метод вызовется, когда элемент будет удален из панели
            Debug.Log("Tab1 detached from panel");
            // Здесь можно отписаться от глобальных событий
        }

        public DashboardPanel(CancellationToken cancellationToken) : base(cancellationToken)
        {
        }

        protected override UniTaskVoid OnPanelStart(CancellationToken cancellationToken)
        {
            // Здесь можно инициализировать элемент
            var label = new Label("Dashboard tab content");
            Add(label);
            return new UniTaskVoid();
        }
    }
}