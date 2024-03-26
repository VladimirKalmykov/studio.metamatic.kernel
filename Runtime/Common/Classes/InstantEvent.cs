using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public class InstantEvent
    {
        public delegate void EmitHandler();
        public delegate void EventListener();
        private static event EventListener OnEmitted;
        public static void Emit(EmitHandler handler)
        {
            handler();
            OnEmitted?.Invoke();
        }

        public static DisposableAction Subscribe(EventListener listener)
        {
            OnEmitted += listener;

            return new DisposableAction(() =>
            {
                OnEmitted -= listener;
            });
        }
    }
    
    public class InstantEvent<T>
    where T: struct
    {
        public delegate void EmitHandler(ref T value);
        public delegate void EventListener(ref T value);
        private static T value;
        private static event EventListener OnEmitted;

        public static void Emit(T nextValue)
        {
            value = nextValue;
        }
        public static void Emit(EmitHandler handler)
        {
            handler(ref value);
            OnEmitted?.Invoke(ref value);
        }

        public static DisposableAction Subscribe(EventListener listener)
        {
            OnEmitted += listener;

            return new DisposableAction(() =>
            {
                OnEmitted -= listener;
            });
        }
        
        /// <summary>
        /// Waits for the specified value to be emitted.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static UniTask WaitFor(T value, CancellationToken cancellationToken = default)
        {
            var taskCompletionSource = new UniTaskCompletionSource();
            CancellationTokenRegistration cancellationTokenRegistration = default;
            var subscription = Subscribe((ref T v) =>
            {
                if (v.Equals(value))
                {
                    cancellationTokenRegistration.Dispose();
                    taskCompletionSource.TrySetResult();
                }
            });
            cancellationTokenRegistration = cancellationToken.Register(() =>
            {
                taskCompletionSource.TrySetCanceled();
                subscription.Dispose();
            });
            return taskCompletionSource.Task;
        }
    }
}