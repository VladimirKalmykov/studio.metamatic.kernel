namespace Metamatic.UI {
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.EventSystems;

    public class PointerEvents: MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler {
        [System.Serializable]
        public class PointerEvent: UnityEvent<PointerEventData> {}
        public PointerEvent OnDown = new PointerEvent();
        public PointerEvent OnUp = new PointerEvent();
        public PointerEvent OnEnter = new PointerEvent();
        public PointerEvent OnExit = new PointerEvent();
        public PointerEvent OnClick = new PointerEvent();
        public void OnPointerDown(PointerEventData eventData)
        {
            OnDown.Invoke(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            OnUp.Invoke(eventData);
        }

        public void OnPointerClick(PointerEventData eventData) {
            OnClick.Invoke(eventData);
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) {
            OnEnter.Invoke(eventData);
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData) {
            OnExit.Invoke(eventData);
        }
    }
}