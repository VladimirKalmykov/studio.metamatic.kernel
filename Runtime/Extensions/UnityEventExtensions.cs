namespace Metamatic
{
    using UnityEngine.Events;

    public static class UnityEventExtensions
    {
        public static DisposableAction Subscribe<T>(this UnityEvent<T> unityEvent, UnityAction<T> action) where T : struct
        {
            unityEvent.AddListener(action);
            return new DisposableAction(() => { unityEvent.RemoveListener(action); });
        }

        public static DisposableAction Subscribe(this UnityEvent unityEvent, UnityAction action)
        {
            unityEvent.AddListener(action);
            return new DisposableAction(() => { unityEvent.RemoveListener(action); });
        }
    }
}