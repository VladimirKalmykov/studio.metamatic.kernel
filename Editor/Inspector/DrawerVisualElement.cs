using System;
using System.Configuration;
using System.Threading;
using Cysharp.Threading.Tasks;
using Metamatic;
using Metamatic.EditorTools;
using UnityEngine;

namespace Metamatic.Inspector
{
    public abstract class DrawerVisualElement : AsyncVisualElementLegacy
    {
        public static DrawerVisualElement Create(Type type, bool editable, CancellationSignal signal = default)
        {
            var drawerType = VisualElementDrawerAttribute.GetDrawer(type);
            if (drawerType == null)
            {
                return new ObjectDrawerVisualElement(editable, signal);
            }
            return (DrawerVisualElement) Activator.CreateInstance(drawerType, editable, signal);
        }
        public DrawerVisualElement(CancellationSignal signal = default) : base(signal)
        {
        }

        public abstract void SetObjectValue(object value);
    }
    public abstract class DrawerVisualElement<T>: DrawerVisualElement
    {
        private CancellationSignalSource m_RenderCancellationSignalSource;
        protected event Action OnReleaseRender;
        private bool m_Editable;

        public bool editable
        {
            get => m_Editable;
            set
            {
                if (value != m_Editable)
                {
                    m_Editable = value;
                    m_RenderCancellationSignalSource.TryCancel();
                    OnReleaseRender?.Invoke();
                    m_RenderCancellationSignalSource = CancellationSignalSource.Create();
                    OnRender(Empty, editable, m_RenderCancellationSignalSource.Signal);
                }
            }
        }
        private static void Empty() { }
        public T value
        {
            set => SetValue(value);
        }
        public DrawerVisualElement(bool editable, CancellationSignal signal = default) : base(signal)
        {
            m_Editable = editable;
        }

        protected override async UniTask InitializeAsync(Func<UniTask> ready, CancellationSignal signal)
        {
            m_RenderCancellationSignalSource = CancellationSignalSource.CreateLinked(signal);
            var renderReadyTask = SafeUniTaskCompletionSource.Create();
            OnRender(() => renderReadyTask.TrySetResult(), m_Editable, m_RenderCancellationSignalSource.Signal)
                .Then(() => renderReadyTask.TrySetResult())
                .Catch(e => renderReadyTask.TrySetException(e))
                .Forget();
            try
            {
                await renderReadyTask.Task;
                await ready();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                m_RenderCancellationSignalSource.TryCancel();
                OnReleaseRender?.Invoke();
            }
        }
        
        public override void SetObjectValue(object value)
        {
            if (value is T t)
            {
                SetValue(t);
            }
        }
        
        public abstract void SetValue(T value);
        public abstract UniTask OnRender(Action ready, bool editable, CancellationSignal cancellationSignal);
        public abstract DisposableAction OnValueChanged(Action<T> action);
    }
}