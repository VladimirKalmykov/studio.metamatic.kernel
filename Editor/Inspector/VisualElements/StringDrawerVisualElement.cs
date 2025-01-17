using System;
using Cysharp.Threading.Tasks;
using UnityEngine.UIElements;

namespace Metamatic.Inspector
{
    [VisualElementDrawer(typeof(string))]
    public class StringDrawerVisualElement: DrawerVisualElement<string>
    {
        private TextField m_TextField;
        private Label m_Label;
        public StringDrawerVisualElement(bool editable, Signal signal) : base(editable, signal)
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

        public override async UniTask OnRender(Action ready, bool editable, Signal cancellationSignal)
        {
            if (editable)
            {
                m_TextField = new TextField();
                Add(m_TextField);
            }
            else
            {
                m_Label = new Label(); // This is a comment, it should be removed
                Add(m_Label);
            }

            ready();
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
    }
}