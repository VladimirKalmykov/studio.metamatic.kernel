using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Metamatic;
using UnityEngine.UIElements;

namespace Metamatic.Inspector
{
    [VisualElementDrawer(typeof(string))]
    public class StringDrawerVisualElement: DrawerVisualElement<string>
    {
        private TextField m_TextField;
        private Label m_Label;
        public StringDrawerVisualElement(bool editable, CancellationSignal signal) : base(editable, signal)
        {
            
        }

        public override void SetValue(string value)
        {
            if (editable)
            {
                m_TextField.value = value;
            }
            else
            {
                m_Label.text = value;
            }
        }

        public override async UniTaskVoid OnRender(bool editable, CancellationSignal cancellationSignal)
        {
            if (editable)
            {
                m_TextField = new TextField();
                Add(m_TextField);
            }
            else
            {
                m_Label = new Label();
                Add(m_Label);
            }

            await cancellationSignal;
            if (editable)
            {
                Remove(m_TextField);
            }
            else
            {
                Remove(m_Label);
            }
        }

        public override DisposableAction OnValueChanged(Action<string> action)
        {
            void Handler(ChangeEvent<string> evt)
            {
                action?.Invoke(evt.newValue);
            }
            m_TextField.RegisterCallback<ChangeEvent<string>>(Handler);
            
            void Dispose()
            {
                OnReleaseRender -= Dispose;
                m_TextField?.UnregisterCallback<ChangeEvent<string>>(Handler);
            }
            OnReleaseRender += Dispose;
            return new DisposableAction(Dispose);
        }

        protected override UniTaskVoid InitializeAsync(CancellationSignal signal)
        {
            throw new NotImplementedException();
        }
    }
}