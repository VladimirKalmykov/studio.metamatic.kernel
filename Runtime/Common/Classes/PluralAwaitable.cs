using System;
using System.Runtime.CompilerServices;
using Metamatic.Effects;

namespace Metamatic
{
    public class PluralAwaitable<T>: ICriticalNotifyCompletion
    {
        public IEffectLegacy.EffectExecutionContext context;
        private bool m_IsCompleted;
        public bool IsCompleted
        {
            get
            {
                return m_IsCompleted;
            }
        }
        
        public T value { get; private set; }
        
        public Action OnCompletedListeners;

        public PluralAwaitable()
        {
            
        }

        public PluralAwaitable(T initialValue)
        {
            value = initialValue;
            m_IsCompleted = true;
        }

        private void Reset()
        {
            m_IsCompleted = false;
            OnCompletedListeners = null;
            value = default;
        }

        public void SetResult(T value)
        {
            m_IsCompleted = true;
            this.value = value;
            
            /*
             * Store handler in local variable to nullify current, because next handler will be specified immediately after invoke
             * @fix 17-5-22
             */
            var handler = OnCompletedListeners;
            OnCompletedListeners = null;
            handler?.Invoke();
        }

        public T GetResult()
        {
            var cachedValue = value;
            Reset();
            return cachedValue;
        }

        public void OnCompleted(Action continuation)
        {
            // ReSharper disable once MergeIntoNegatedPattern
            if (IsCompleted)
            {
                continuation();
                return;
            }
            
            #if DEBUG
            if (OnCompletedListeners != null)
            {
                throw new Exception("You can not subscribe twice!");
            }
            #endif
            OnCompletedListeners = continuation;
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            // ReSharper disable once MergeIntoNegatedPattern
            if (IsCompleted)
            {
                continuation();
                return;
            }
            
            #if DEBUG
            if (OnCompletedListeners != null)
            {
                throw new Exception("You can not subscribe twice!");
            }
            #endif
            OnCompletedListeners = continuation;
        }
            
        public PluralAwaitable<T> GetAwaiter()
        {
            return this;
        }
    }
}