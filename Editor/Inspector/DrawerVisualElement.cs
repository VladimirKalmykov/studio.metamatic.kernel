using System;
using System.Configuration;
using System.Threading;
using Cysharp.Threading.Tasks;
using Metamatic;
using Metamatic.EditorTools;

namespace Metamatic.Inspector
{
    public abstract class DrawerVisualElement : AsyncVisualElement
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
                    OnRender(editable, m_RenderCancellationSignalSource.Signal);
                }
            }
        }
        public T value
        {
            set => SetValue(value);
        }
        public DrawerVisualElement(bool editable, CancellationSignal signal = default) : base(signal)
        {
            m_Editable = editable;
        }

        protected override async UniTaskVoid InitializeAsync(CancellationSignal signal)
        {
            m_RenderCancellationSignalSource = CancellationSignalSource.Create();
            OnRender(editable, m_RenderCancellationSignalSource.Signal);
            await signal;
            m_RenderCancellationSignalSource.TryCancel();
            OnReleaseRender?.Invoke();
        }
        
        public override void SetObjectValue(object value)
        {
            if (value is T t)
            {
                SetValue(t);
            }
        }
        
        public abstract void SetValue(T value);
        public abstract UniTaskVoid OnRender(bool editable, CancellationSignal cancellationSignal);
        public abstract DisposableAction OnValueChanged(Action<T> action);
    }
}