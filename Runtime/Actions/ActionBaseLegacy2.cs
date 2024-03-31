using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Object = System.Object;

namespace Metamatic.Actions
{
    public abstract class ActionBaseLegacy2
    {
#if DEBUG
        private Object m_Owner;
        public Object owner => m_Owner;
        private float m_CreatedTime;
        public float createdTime => m_CreatedTime;
#endif
        public string GetDebugMessage()
        {
#if DEBUG
            return $"{owner} {this.GetType().Name}";
            #else
            return $"{this.GetType().Name}";
#endif
        }

        #if DEBUG
        protected void OnRecreate(Object owner)
        {
            m_Owner = owner;
            m_CreatedTime = Time.time;
        }
        #endif

        public void Clear()
        {
            #if DEBUG
            m_Owner = null;
            m_CreatedTime = 0;
            #endif
            OnClear();
        }

        protected abstract void OnClear();
    }
    
    [Obsolete("Concept of static action is incorrect and has critical issues. Do not use it.", true)]
    public class ActionLeagcy2: ActionBaseLegacy2, IDisposable
    {
        private static ActionLeagcy2 m_Instance;

        public delegate void EventHandler();

        public static event EventHandler OnEmit;

        public static ActionLeagcy2 Emit(Object owner = null)
        {
            if (m_Instance == null)
            {
                m_Instance = new ActionLeagcy2();
            }
            m_Instance.InternalEmit();
            #if DEBUG
            m_Instance.OnRecreate(owner);
            #endif
            return m_Instance;
        }

        private void InternalEmit()
        {
            OnEmit?.Invoke();
        }

        public ActionLeagcy2()
        {
            if (m_Instance != null)
            {
                throw new Exception("Action is a static singleton. Do not create its instances.");
            }
        }

        public void Dispose()
        {
            // Dispose
        }

        public static DisposableAction Subscribe(EventHandler handler)
        {
            OnEmit += handler;

            return new DisposableAction(() =>
            {
                OnEmit -= handler;
            });
        }

        protected sealed override void OnClear()
        {
            // Do nothing
        }
    }
    
    
    /// <summary>
    /// Action is a singleton class that can be used to pass data between systems.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Obsolete("Concept of static action is incorrect and has critical issues. Do not use it.", true)]
    public class ActionLegacy2<T>: ActionBaseLegacy2, IDisposable
    {
        private static ActionLegacy2<T> m_Instance;

        public delegate void EventHandler(T value);

        public static event EventHandler OnEmit;

        public static ActionLegacy2<T> Emit(T value, Object owner = null)
        {
            if (m_Instance == null)
            {
                m_Instance = new ActionLegacy2<T>();
            }
            m_Instance.InternalEmit(value);
            #if DEBUG
            m_Instance.OnRecreate(owner);
            #endif
            return m_Instance;
        }

        private void InternalEmit(T value)
        {
            m_Instance.m_Value = value;
            OnEmit?.Invoke(value);
        }
        
        private T m_Value;
        public T value => m_Value;
        
        

        public ActionLegacy2()
        {
            if (m_Instance != null)
            {
                throw new Exception("Action<T> is a static singleton. Do not create its instances.");
            }
        }

        public void Dispose()
        {
            // Dispose
        }

        public static DisposableAction Subscribe(EventHandler handler)
        {
            OnEmit += handler;

            return new DisposableAction(() =>
            {
                OnEmit -= handler;
            });
        }

        protected sealed override void OnClear()
        {
            m_Value = default(T);
        }
    }
}