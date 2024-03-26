using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Object = System.Object;

namespace Metamatic.Actions
{
    public abstract class Action
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
    
    /// <summary>
    /// Action is a singleton class that can be used to pass data between systems.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Action<T>: Action, IDisposable
    {
        private static Action<T> m_Instance;

        public delegate void EventHandler(T value);

        public static event EventHandler OnEmit;

        public static Action<T> Emit(T value, Object owner = null)
        {
            if (m_Instance == null)
            {
                m_Instance = new Action<T>();
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
        
        

        public Action()
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

        public static System.Action Subscribe(EventHandler handler)
        {
            OnEmit += handler;

            return () =>
            {
                OnEmit -= handler;
            };
        }

        protected sealed override void OnClear()
        {
            m_Value = default(T);
        }
    }
}