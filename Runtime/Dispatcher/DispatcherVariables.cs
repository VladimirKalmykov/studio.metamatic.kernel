using System;
using System.Collections.Generic;

namespace Metamatic.Actions
{
    public partial class Dispatcher
    {
        public readonly struct VariableSetter<T>: IDisposable
        {
            public delegate void ActionOnRelease(object key, Type type, string name);
            private readonly object m_Key;
            private readonly string m_Name;
            private readonly MicroState<T> m_MicroState;
            private readonly Action m_OnDispose1;
            private readonly ActionOnRelease m_OnDispose2;

            public VariableSetter(object key, string name, MicroState<T> mictoState, Inquirable inquirable, ActionOnRelease actionOnRelease)
            {
                m_Key = key;
                m_Name = name;
                m_MicroState = mictoState;
                m_OnDispose1 = inquirable.Inquire();
                m_OnDispose2 = actionOnRelease;
            }

            public void Set(T value)
            {
                m_MicroState.value = value;
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
                var microStateBase = new MicroState<T>();
                inquirable = new Inquirable<MicroState<T>>(microStateBase);
                m_Variables.Add(dictionaryKey, inquirable);
            }
            
            var inquirableTyped = (Inquirable<MicroState<T>>)inquirable;
            var microState = (MicroStateReadOnly<T>)inquirableTyped.value;
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
                var microStateBase = new MicroState<T>();
                inquirable = (Inquirable)new Inquirable<MicroState<T>>(microStateBase);
                m_Variables.Add(dictionaryKey, inquirable);
            }
            
            var inquirableTyped = (Inquirable<MicroState<T>>)inquirable;
            var microState = inquirableTyped.value;

            return new VariableSetter<T>(key, name, microState, inquirable, CheckForVariableReadyToRelease);
        }

        public VariableSetter<T> GetVariableSetter<T> (string name)
        {
            return GetVariableSetter<T>(this, name);
        }
    }
}