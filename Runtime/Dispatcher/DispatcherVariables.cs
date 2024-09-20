using System;
using System.Collections.Generic;

namespace Metamatic
{
    public partial class Dispatcher
    {
        public readonly struct VariableSetter<T>: IDisposable
        {
            public delegate void ActionOnRelease(object key, Type type, string name);
            private readonly object m_Key;
            private readonly string m_Name;
            private readonly Observable<T> m_Observable;
            private readonly Action m_OnDispose1;
            private readonly ActionOnRelease m_OnDispose2;

            public VariableSetter(object key, string name, Observable<T> mictoState, Inquirable inquirable, ActionOnRelease actionOnRelease)
            {
                m_Key = key;
                m_Name = name;
                m_Observable = mictoState;
                m_OnDispose1 = inquirable.Inquire();
                m_OnDispose2 = actionOnRelease;
            }

            public void Set(T value)
            {
                m_Observable.value = value;
            }

            public void Dispose()
            {
                m_OnDispose1();
                m_OnDispose2(m_Key, typeof(T), m_Name);
            }
            
            public static implicit operator Action<T>(VariableSetter<T> variableSetter)
            {
                return variableSetter.Set;
            }
        }
        private readonly IDictionary<(object, Type, string), Inquirable> m_Variables = new Dictionary<(object, Type, string), Inquirable>();
        
        public DisposableAction SubscribeVariable<T>(object key, string name, Action<T> action, T fallbackValue = default)
        {
            var type = typeof(T);
            var dictionaryKey = (key, type, name);
            var isNew = false;
            if (!m_Variables.TryGetValue(dictionaryKey, out var inquirable))
            {
                isNew = true;
                var microStateBase = new Observable<T>();
                inquirable = new Inquirable<Observable<T>>(microStateBase);
                m_Variables.Add(dictionaryKey, inquirable);
            }
            
            var inquirableTyped = (Inquirable<Observable<T>>)inquirable;
            var microState = (ObservableReadOnly<T>)inquirableTyped.value;
            action(isNew ? fallbackValue : microState.value);
            var subscription = microState.Subscribe(action);
            var disposable = inquirableTyped.Inquire();
            return new DisposableAction(() =>
            {
                subscription.Dispose();
                disposable.Dispose();
                CheckForVariableReadyToRelease(key, type, name);
            });
        }
        
        public DisposableAction SubscribeVariable<T>(string name, Action<T> action, T fallbackValue = default)
        {
            return SubscribeVariable(this, name, action);
        }
        
        private void CheckForVariableReadyToRelease(object key, Type type, string name)
        {
            var dictionaryKey = (key, type, name);
            if (m_Variables.TryGetValue(dictionaryKey, out var inquirable))
            {
                if (inquirable.inquireCount == 0)
                {
                    m_Variables.Remove(dictionaryKey);
                }
            }
        }
        
        public VariableSetter<T> GetVariableSetter<T>(object key, string name)
        {
            var type = typeof(T);
            var dictionaryKey = (key, type, name);
            if (!m_Variables.TryGetValue(dictionaryKey, out var inquirable))
            {
                var microStateBase = new Observable<T>();
                inquirable = (Inquirable)new Inquirable<Observable<T>>(microStateBase);
                m_Variables.Add(dictionaryKey, inquirable);
            }
            
            var inquirableTyped = (Inquirable<Observable<T>>)inquirable;
            var microState = inquirableTyped.value;

            return new VariableSetter<T>(key, name, microState, inquirable, CheckForVariableReadyToRelease);
        }

        public VariableSetter<T> GetVariableSetter<T> (string name)
        {
            return GetVariableSetter<T>(this, name);
        }
        
        /// <summary>
        /// Read variable from the dispatcher. Use it only if you are sure that variable exists. In most cases you should use SubscribeVariable instead.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="name"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public T ReadVariable<T>(object key, string name)
        {
            var type = typeof(T);
            var dictionaryKey = (key, type, name);
            if (!m_Variables.TryGetValue(dictionaryKey, out var inquirable))
            {
                throw new KeyNotFoundException($"Variable {name} not found for key {key}");
            }
            
            var inquirableTyped = (Inquirable<Observable<T>>)inquirable;
            var microState = inquirableTyped.value;
            return microState.value;
        }
        
        /// <summary>
        /// Read variable from the dispatcher (using dispatcher as a variable owner)
        /// </summary>
        /// <param name="name"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T ReadVariable<T>(string name)
        {
            return ReadVariable<T>(this, name);
        }
    }
}