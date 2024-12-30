using Cysharp.Threading.Tasks;
using Metamatic.UIElements;
using Metamatic.UIToolKit;
using UnityEngine.UIElements;

namespace Metamatic.Framework
{
    public class CriticalNotification: AsyncVisualElement
    {
        public enum MessageType
        {
            Info,
            Error
        }
        private Label m_Label;
        private int m_Iteration;
        public Observable<(int, string, float, MessageType)> m_Showing = new Observable<(int, string, float, MessageType)>();
        
        public CriticalNotification()
        {
            AddToClassList(UIToolKitCore.DarkEnoughClassName);
            AddToClassList("MetamaticOverlay_CriticalNotification");
            AddToClassList(UIToolKitCore.Tailwind.PositionAbsolute);
            m_Label = new Label();
            m_Label.AddToClassList(UIToolKitCore.Tailwind.TextSizeXl);
            Add(m_Label);
            style.opacity = 0;
            style.translate = new StyleTranslate(new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent)));
            style.display = DisplayStyle.None;
            style.top = new StyleLength(new Length(50, LengthUnit.Percent));
            style.left = new StyleLength(new Length(50, LengthUnit.Percent));
        }

        public void Show(string message, float duration = 2f, MessageType messageType = MessageType.Error)
        {
            unchecked
            {
                m_Iteration++;
            }
            m_Showing.Value = (m_Iteration, message, duration, messageType);
        }

        public DisposableAction Show(string message, MessageType messageType)
        {
            unchecked
            {
                m_Iteration++;
            }
            var thisIteration = m_Iteration;
            m_Showing.Value = (m_Iteration, message, 0f, messageType);
            
            return new DisposableAction(() =>
            {
                if (m_Iteration == thisIteration)
                {
                    m_Showing.Value = (m_Iteration, string.Empty, 0f, messageType);
                }
            });
        }

        public override async UniTask OnAttachedAsync(Signal detachSignal)
        {
            using var showing = m_Showing.WatchAsync(OnShowChanged);
            // Subscribe to geometry changes
            RegisterCallback<GeometryChangedEvent>(OnGeometryChange);
            await detachSignal;
            RegisterCallback<GeometryChangedEvent>(OnGeometryChange);
        }
        
        private void OnGeometryChange(GeometryChangedEvent evt)
        {
            
        }
        
        private async UniTaskVoid OnShowChanged((int, string, float, MessageType) showing, Signal signal)
        {
            var (iteration, message, duration, messageType) = showing;
            
            
            style.opacity = 0;
            if (string.IsNullOrEmpty(message))
            {
                style.display = DisplayStyle.None;
                return;
            }
            style.display = DisplayStyle.Flex;
            ApplyMessageType(messageType);
            m_Label.text = message;
            await PlayerLoop.EachFrame((float progress) =>
            {
                style.opacity = progress;
            }, 0.34f, PlayerLoopTiming.Update, signal);
            if (duration > 0f)
            {
                await PlayerLoop.Delay(duration, signal: signal);
                await PlayerLoop.EachFrame((float progress) =>
                {
                    style.opacity = 1f - progress;
                }, 0.34f, PlayerLoopTiming.Update, signal);
                style.display = DisplayStyle.None;
            }
            else
            {
                // Wait for the next iteration
                await signal;
            }            
        }
        
        public void ApplyMessageType(MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.Info:
                    RemoveFromClassList("Notification_Error");
                    AddToClassList("Notification_Info");
                    break;
                case MessageType.Error:
                    RemoveFromClassList("Notification_Info");
                    AddToClassList("Notification_Error");
                    break;
            }
        }
    }
}