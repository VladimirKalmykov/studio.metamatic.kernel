namespace Metamatic
{
    using UnityEngine.Events;
    using System;

    public static class UnityEventExtensions
    {
        public static Action Subscribe<T>(this UnityEvent<T> unityEvent, UnityAction<T> action) where T : struct
        {
            unityEvent.AddListener(action);
            return () => { unityEvent.RemoveListener(action); };
        }

        public static Action Subscribe(this UnityEvent unityEvent, UnityAction action)
        {
            unityEvent.AddListener(action);
            return () => { unityEvent.RemoveListener(action); };
        }
    }
}