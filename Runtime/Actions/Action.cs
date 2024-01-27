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
            return $"<color=cyan>ACTION:</color> {owner} {this.GetType().Name}";
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

        public static Action<T> Create(T value, Object owner = null)
        {
            if (m_Instance == null)
            {
                m_Instance = new Action<T>(value);
            }
            else
            {
                m_Instance.m_Value = value;
            }
            #if DEBUG
            m_Instance.OnRecreate(owner);
            #endif
            return m_Instance;
        }
        
        private T m_Value;
        public T value => m_Value;
        
        

        public Action(T value)
        {
            if (m_Instance != null)
            {
                throw new Exception("Action<T> is a singleton. Use Action<T>.Create instead.");
            }
            this.m_Value = value;
        }

        public void Dispose()
        {
            // Dispose
        }

        protected override void OnClear()
        {
            m_Value = default(T);
        }
    }
}