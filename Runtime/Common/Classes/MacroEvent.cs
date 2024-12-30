using Metamatic;
using UnityEngine;

namespace Metamatic
{
    public static class MacroEvent<T>
    {
        private static event System.Action<T, Signal> Event;

        public static void Emit(T type, Signal cancellationSignal = default)
        {
            Console.Info("MacroEvent", typeof(T).Name, $"Emit {type}");
            Event?.Invoke(type, cancellationSignal);
        }
        
        public static DisposableAction Subscribe(System.Action<T, Signal> action)
        {
            Event += action;
            return new DisposableAction(() => Event -= action);
        }
        
        public static DisposableAction SubscribeOnce(System.Action<T, Signal> action)
        {
            void Handler(T data, Signal signal)
            {
                action(data, signal);
                Event -= Handler;
            }
            Event += Handler;
            return new DisposableAction(() => Event -= Handler);
        }
    }
}