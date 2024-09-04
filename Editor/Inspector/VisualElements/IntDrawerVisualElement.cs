using System;
using Cysharp.Threading.Tasks;
using UnityEngine.UIElements;

namespace Metamatic.Inspector
{
    [VisualElementDrawer(typeof(int))]
    public class IntDrawerVisualElement: DrawerVisualElement<int>
    {
        private IntegerField m_IntField;
        private Label m_Label;
        public IntDrawerVisualElement(bool editable, CancellationSignal signal = default) : base(editable, signal)
        {
        }

        public override void SetValue(int value)
        {
            if (editable)
            {
                m_IntField.value = value;
            }
            else
            {
                m_Label.text = value.ToString();
            }
        }

        public override async UniTask OnRender(Action ready, bool editable, CancellationSignal cancellationSignal)
        {
            if (editable)
            {
                m_IntField = new IntegerField();
                Add(m_IntField);
            }
            else
            {
                m_Label = new Label();
                Add(m_Label);
            }

            ready();
            await cancellationSignal;
            
            if (editable)
            {
                Remove(m_IntField);
            }
            else
            {
                Remove(m_Label);
            }
            
        }

        public override DisposableAction OnValueChanged(Action<int> action)
        {
            void Handler(ChangeEvent<int> evt)
            {
                action?.Invoke(evt.newValue);
            }

            if (editable)
            {
                m_IntField.RegisterValueChangedCallback(Handler);
            }
            return new DisposableAction(() =>
            {
                if (editable)
                {
                    m_IntField.UnregisterValueChangedCallback(Handler);
                }
            });
        }
    }
}