using System;
using System.Collections.Generic;

namespace Metamatic
{
    /// <summary>
    /// CompositeAction позволяет объединить несколько Action в один.
    /// </summary>
    public readonly struct CompositeAction
    {
        private readonly IEnumerable<Action> actions;
        
        public CompositeAction(params Action[] actions)
        {
            this.actions = actions;
        }
        
        public void Invoke()
        {
            foreach (var action in actions)
            {
                action?.Invoke();
            }
        }
        
        // Приведение к типу Action
        public static implicit operator Action(CompositeAction compositeAction)
        {
            return compositeAction.Invoke;
        }
    }
    
    /// <summary>
    /// CompositeActionDisposable позволяет объединить Action<T> и несколько IDisposable в один Action<T>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public readonly struct CompositeActionDisposable<T>
    {
        private readonly Action<T> action;
        private readonly IEnumerable<IDisposable> disposables;
        
        public CompositeActionDisposable(Action<T> first, params IDisposable[] disposables)
        {
            this.action = first;
            this.disposables = disposables;
        }
        
        public void Invoke(T arg)
        {
            action?.Invoke(arg);
            foreach (var a in disposables)
            {
                a?.Dispose();
            }
        }
        
        // Приведение к типу Action<T>
        public static implicit operator Action<T>(CompositeActionDisposable<T> compositeActionDisposable)
        {
            return compositeActionDisposable.Invoke;
        }
    }
}