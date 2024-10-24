using System.Runtime.CompilerServices;
using UnityEngine;

namespace Metamatic
{
    using System;
    using System.Collections.Generic;

    public class Validator : IDisposable
    {
        public delegate bool ValidatorDelegate();
        public event Action OnChange;

        private class LinkedValidator
        {
            private static bool DefaultValidator() => true;

            private ValidatorDelegate m_Handler = DefaultValidator;
            public LinkedValidator Parent;
            public LinkedValidator Child;

            public void Init(LinkedValidator parent, ValidatorDelegate handler)
            {
                m_Handler = handler;
                parent?.SetChild(this);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetChild(LinkedValidator next)
            {
                Child = next;
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
                Parent?.SetChild(Child);
                Parent = null;
                Child = null;
            }

            public bool Validate()
            {
                return m_Handler() && (Parent?.Validate() ?? true);
            }

            public ValidatorDelegate[] GetValidationFunction()
            {
                var functions = new List<ValidatorDelegate>();
                var current = this;
                while (current != null)
                {
                    functions.Add(current.m_Handler);
                    current = current.Parent;
                }

                return functions.ToArray();
            }
        }

        private readonly Queue<LinkedValidator> m_Pool = new();
        private LinkedValidator m_Current;
        private ValidatorDelegate m_CurrentHandler;

        public DisposableAction Add(ValidatorDelegate validator)
        {
            var newValidator = m_Pool.Count > 0 ? m_Pool.Dequeue() : new LinkedValidator();
            newValidator.Init(m_Current, validator);
            m_Current = newValidator;
            OnChange?.Invoke();

            return new DisposableAction(() =>
            {
                m_Current = newValidator.Child ?? newValidator.Parent;
                newValidator.Release();
                // TODO: Consider adding a limit to the pool size
                m_Pool.Enqueue(newValidator);
                OnChange?.Invoke();
#if DEBUG
                if (m_Pool.Count > 20)
                {
                    throw new Exception("Pool size exceeded");
                }
#endif
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Validate()
        {
            return m_Current?.Validate() ?? true;
        }
        
        public bool isValid => Validate();

        public void Dispose()
        {
            m_Pool.Clear();
            m_Current?.Release();
            m_Current = null;
        }
    }

    public class Validator<T> : IDisposable
    {
        public delegate bool ValidatorDelegate(T item);

        private class LinkedValidator
        {
            private static bool DefaultValidator(T item) => true;
            public ValidatorDelegate Handler = DefaultValidator;
            public LinkedValidator Parent;
            public LinkedValidator Child;

            public void Init(LinkedValidator parent, ValidatorDelegate handler)
            {
                Handler = handler;
                parent?.SetChild(this);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetChild(LinkedValidator next)
            {
                Child = next;
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
                Parent?.SetChild(Child);
                Parent = null;
                Child = null;
            }

            public bool Validate(T item)
            {
                return Handler(item) && (Parent?.Validate(item) ?? true);
            }

            public ValidatorDelegate[] GetValidationFunction()
            {
                var functions = new List<ValidatorDelegate>();
                var current = this;
                while (current != null)
                {
                    functions.Add(current.Handler);
                    current = current.Parent;
                }

                return functions.ToArray();
            }
        }

        private readonly Queue<LinkedValidator> m_Pool = new();
        private LinkedValidator m_Current;
        private ValidatorDelegate m_CurrentHandler;

        public DisposableAction Add(ValidatorDelegate validator)
        {
            var newValidator = m_Pool.Count > 0 ? m_Pool.Dequeue() : new LinkedValidator();
            newValidator.Init(m_Current, validator);
            m_Current = newValidator;

            return new DisposableAction(() =>
            {
                m_Current = newValidator.Child ?? newValidator.Parent;
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

    public class Validator<T1, T2> : IDisposable
    {
#if DEBUG
        public bool debug = false;
#endif
        public delegate bool ValidatorDelegate(T1 item1, T2 item2);

        private class LinkedValidator
        {
            private static bool DefaultValidator(T1 item1, T2 item2) => true;
            public ValidatorDelegate Handler = DefaultValidator;
            public LinkedValidator Parent;
            public LinkedValidator Child;

            public void Init(LinkedValidator parent, ValidatorDelegate handler)
            {
                Handler = handler;
                parent?.SetChild(this);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetChild(LinkedValidator next)
            {
                Child = next;
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
                Parent?.SetChild(Child);
                Parent = null;
                Child = null;
            }

            public bool Validate(T1 item1, T2 item2)
            {
                return Handler(item1, item2) && (Parent?.Validate(item1, item2) ?? true);
            }

            public ValidatorDelegate[] GetValidationFunction()
            {
                var functions = new List<ValidatorDelegate>();
                var current = this;
                while (current != null)
                {
                    functions.Add(current.Handler);
                    current = current.Parent;
                }

                return functions.ToArray();
            }

#if DEBUG
            public override string ToString()
            {
                return $"{Handler?.Method.Name ?? "null"} -> {Parent?.ToString() ?? "null"}";
            }
#endif
        }

        private readonly Queue<LinkedValidator> m_Pool = new();
        private LinkedValidator m_Current;
        private ValidatorDelegate m_CurrentHandler;

        public DisposableAction Add(ValidatorDelegate validator)
        {
            var newValidator = m_Pool.Count > 0 ? m_Pool.Dequeue() : new LinkedValidator();
            newValidator.Init(m_Current, validator);
#if DEBUG
            if (debug)
            {
                Console.Info("Validator", $"Add new validator {newValidator?.ToString()} and set as children of {m_Current?.ToString()} (Parent: {m_Current?.Parent?.ToString()}, Children: {m_Current?.Child?.ToString()})");
            }

#endif
            m_Current = newValidator;


            return new DisposableAction(() =>
            {
#if DEBUG
                if (debug)
                {
                    global::Metamatic.Console.Info("Validator", 
                        $"Dispose validator {newValidator.ToString()} (Parent: {newValidator.Parent?.ToString()}, Children: {newValidator.Child?.ToString()})");
                }
#endif
                m_Current = newValidator.Child ?? newValidator.Parent;
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

#if DEBUG
        public override string ToString()
        {
            return m_Current?.ToString() ?? "Empty";
        }

        private void LogCurrentThread(string methodName)
        {
            var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            global::Metamatic.Console.Info("Validator", $"Method: {methodName}, Thread ID: {threadId}");
        }
#endif
    }
}