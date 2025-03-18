using System;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace Metamatic
{
    public abstract class ObservableNullableReadOnly<T>: IDisposable
    {
        protected T m_Value;
        [UsedImplicitly] protected bool m_HasValue;
        public event Action<T, bool> OnChange;
        
        // ReSharper disable once InconsistentNaming
        public T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Value;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetValue(value, true);
        }
        
        public T value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Value;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetValue(value, true);
        }

        public bool hasValue
        {
            get => m_HasValue;
            set => SetValue(m_Value, value);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (T, bool) GetValue()
        {
            return (m_Value, m_HasValue);
        }
        
        [UsedImplicitly]
        public DisposableAction Watch(Action<T, bool> action)
        {
            OnChange += action;
            action(m_Value, m_HasValue);
            return new DisposableAction(() =>
            {
                OnChange -= action;
            });
        }

        public DisposableAction WatchAsync(Func<T, bool, Signal, UniTaskVoid> handler)
        {
            SignalSource cancellationSignalSource = default;
            var watcher = Watch((nextValue, nextHasValue) =>
            {
                cancellationSignalSource.TryRaise();
                cancellationSignalSource = SignalSource.Create();
                handler(nextValue, nextHasValue, cancellationSignalSource.Signal).Forget();
            });
            return DisposableAction.Create(() =>
            {
                watcher.Dispose();
                cancellationSignalSource.TryRaise();
            });
        }
        
        protected void ApplyChange(T newValue, bool newHasValue)
        {
            this.m_Value = newValue;
            this.m_HasValue = newHasValue;
            OnChange?.Invoke(newValue, newHasValue);
        }
        
        public virtual void SetValue(T newValue, bool newHasValue)
        {
            Debug.LogError("You can't set value of ObservableNullableReadOnly directly. Use ObservableNullable.SetValue instead.");
        }

        public void Dispose()
        {
            // Apply change to default value to make all subscribers to be notified that the observable is disposed.
            ApplyChange(default, false);
            OnChange = null;
        }
    }

    public class ObservableNullable<T> : ObservableNullableReadOnly<T>
    {
        public ObservableNullable()
        {
            
        }
        
        public ObservableNullable(T initialValue = default)
        {
            m_Value = initialValue;
            m_HasValue = true;
        }
        
        public override void SetValue(T newValue, bool newHasValue)
        {
            ApplyChange(newValue, newHasValue);
        }
        
        public void Clear()
        {
            ApplyChange(default, false);
        }
    }
}