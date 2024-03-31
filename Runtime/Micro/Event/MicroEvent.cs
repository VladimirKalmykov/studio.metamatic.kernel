using System;
using System.Collections.Generic;
using Metamatic;

namespace Micro.Event
{
    public class MicroEvent
    {
        private List<Action> m_EventHandlers = new List<Action>();
        
        public DisposableAction Subscribe(Action handler)
        {
            m_EventHandlers.Add(handler);
            return new DisposableAction(() =>
            {
                m_EventHandlers.Remove(handler);
            });
        }
        
        public void Emit()
        {
            var handlers = m_EventHandlers.ToArray();
            foreach (var handler in handlers)
            {
                handler();
            }
        }
    }
    
    public class MicroEvent<T>
    {
        private List<Action<T>> m_EventHandlers = new List<Action<T>>();
        
        public DisposableAction Subscribe(Action<T> handler)
        {
            m_EventHandlers.Add(handler);
            return new DisposableAction(() =>
            {
                m_EventHandlers.Remove(handler);
            });
        }
        
        public void Emit(T arg)
        {
            var handlers = m_EventHandlers.ToArray();
            foreach (var handler in handlers)
            {
                handler(arg);
            }
        }
    }
}