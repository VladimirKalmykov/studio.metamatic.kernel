using System.Runtime.CompilerServices;
using Sirenix.OdinInspector.Editor.Validation;

namespace Metamatic
{
    using System;
    using System.Collections.Generic;

    public class Validator<T>: IDisposable
    {
        public delegate bool ValidatorDelegate(T item);
        private class LinkedValidator
        {
            public ValidatorDelegate Handler;
            public LinkedValidator Parent;
            private LinkedValidator m_Child;

            public void Init(LinkedValidator parent, ValidatorDelegate handler)
            {
                Handler = handler;
                parent?.SetChild(this);
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetChild(LinkedValidator next)
            {
                m_Child = next;
                next?.SetParent(this);
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetParent(LinkedValidator parent)
            {
                Parent = parent;
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Release()
            {
                Parent?.SetChild(m_Child);
                Parent = null;
                m_Child = null;
            }
            
            public bool Validate(T item)
            {
                return Handler(item) && (Parent?.Validate(item) ?? true);
            }

            public ValidatorDelegate[] GetValidationFunction()
            {
                var functions = new List<ValidatorDelegate>();
                var current = this;
                while (current != null) {
                    functions.Add(current.Handler);
                    current = current.Parent;
                }
                return functions.ToArray();
            
            }
        }
        
        private readonly Queue<LinkedValidator> m_Pool = new ();
        private LinkedValidator m_Current;
        private ValidatorDelegate m_CurrentHandler;
        
        public DisposableAction Add(ValidatorDelegate validator)
        {
            var newValidator = m_Pool.Count > 0 ? m_Pool.Dequeue() : new LinkedValidator();
            newValidator.Init(m_Current, validator);
            m_Current = newValidator;
            
            return new DisposableAction(() =>
            {
                m_Current = newValidator.Parent;
                newValidator.Release();
                // TODO: Consider adding a limit to the pool size
                m_Pool.Enqueue(newValidator);
                #if DEBUG
                if (m_Pool.Count > 20)
                {
                    throw new Exception("Pool size exceeded");
                }
                #endif
            });
        }
       
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Validate(T item)
        {
            return m_Current?.Validate(item) ?? true;
        }

        public void Dispose()
        {
            m_Pool.Clear();
            m_Current?.Release();
            m_Current = null;
        }
    }
    
    public class Validator<T1, T2>: IDisposable
    {
        public delegate bool ValidatorDelegate(T1 item1, T2 item2);
        private class LinkedValidator
        {
            public ValidatorDelegate Handler;
            public LinkedValidator Parent;
            private LinkedValidator m_Child;

            public void Init(LinkedValidator parent, ValidatorDelegate handler)
            {
                Handler = handler;
                parent?.SetChild(this);
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetChild(LinkedValidator next)
            {
                m_Child = next;
                next?.SetParent(this);
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetParent(LinkedValidator parent)
            {
                Parent = parent;
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Release()
            {
                Parent?.SetChild(m_Child);
                Parent = null;
                m_Child = null;
            }
            
            public bool Validate(T1 item1, T2 item2)
            {
                return Handler(item1, item2) && (Parent?.Validate(item1, item2) ?? true);
            }

            public ValidatorDelegate[] GetValidationFunction()
            {
                var functions = new List<ValidatorDelegate>();
                var current = this;
                while (current != null) {
                    functions.Add(current.Handler);
                    current = current.Parent;
                }
                return functions.ToArray();
            
            }
        }
        
        private readonly Queue<LinkedValidator> m_Pool = new ();
        private LinkedValidator m_Current;
        private ValidatorDelegate m_CurrentHandler;
        
        public DisposableAction Add(ValidatorDelegate validator)
        {
            var newValidator = m_Pool.Count > 0 ? m_Pool.Dequeue() : new LinkedValidator();
            newValidator.Init(m_Current, validator);
            m_Current = newValidator;
            
            return new DisposableAction(() =>
            {
                m_Current = newValidator.Parent;
                newValidator.Release();
                // TODO: Consider adding a limit to the pool size
                m_Pool.Enqueue(newValidator);
                #if DEBUG
                if (m_Pool.Count > 20)
                {
                    throw new Exception("Pool size exceeded");
                }
                #endif
            });
        }
       
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Validate(T1 item1, T2 item2)
        {
            return m_Current?.Validate(item1, item2) ?? true;
        }

        public void Dispose()
        {
            m_Pool.Clear();
            m_Current?.Release();
            m_Current = null;
        }
    }
}