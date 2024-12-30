using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Metamatic;
using UnityEngine.UIElements;

namespace Metamatic.Inspector
{
    public class ObjectDrawerVisualElement: DrawerVisualElement<object>
    {
        private Label m_Label;
        public ObjectDrawerVisualElement(bool editable, Signal signal = default) : base(editable, signal)
        {
        }

        public override void SetValue(object value)
        {
            if (m_Label == null)
            {
                throw new Exception("Label is null");
            }
            m_Label.text = value.ToString();
        }

        public override async UniTask OnRender(Action ready, bool editable, Signal cancellationSignal)
        {
            /* This drawer can not be editable */
            m_Label = new Label
            {
                text = ""
            };
            Add(m_Label);
            ready();
            await cancellationSignal;
            Remove(m_Label);
        }

        public override DisposableAction OnValueChanged(Action<object> action)
        {
            return DisposableAction.Empty;
        }
    }
}