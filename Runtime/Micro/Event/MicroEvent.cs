using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic
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
        
        public DisposableAction SubscribeOnce(Action handler)
        {
            Action onceHandler = null;
            onceHandler = () =>
            {
                handler();
                m_EventHandlers.Remove(onceHandler);
            };
            m_EventHandlers.Add(onceHandler);
            return new DisposableAction(() =>
            {
                m_EventHandlers.Remove(onceHandler);
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
        
        public UniTask WaitUntilEmitted(CancellationToken cancellationToken)
        {
            var tcs = new UniTaskCompletionSource();
            Action handler = null;
            handler = () =>
            {
                tcs.TrySetResult();
                m_EventHandlers.Remove(handler);
            };
            m_EventHandlers.Add(handler);
            cancellationToken.Register(() =>
            {
                tcs.TrySetCanceled();
                m_EventHandlers.Remove(handler);
            });
            return tcs.Task;
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// TODO: Should check this implementation for performance issues.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public class MicroEvent<T>
    {
        private List<Action<T>> m_EventHandlers = new List<Action<T>>();
        
        public bool isEmpty => m_EventHandlers.Count == 0;
        
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
        
        public UniTask<T> WaitUntilEmitted(CancellationToken cancellationToken)
        {
            var tcs = new UniTaskCompletionSource<T>();
            Action<T> handler = null;
            handler = arg =>
            {
                tcs.TrySetResult(arg);
                m_EventHandlers.Remove(handler);
            };
            m_EventHandlers.Add(handler);
            cancellationToken.Register(() =>
            {
                tcs.TrySetCanceled();
                m_EventHandlers.Remove(handler);
            });
            return tcs.Task;
        }
    }
}