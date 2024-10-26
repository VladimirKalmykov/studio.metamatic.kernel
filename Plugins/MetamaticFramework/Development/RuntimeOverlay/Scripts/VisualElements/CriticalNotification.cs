using Cysharp.Threading.Tasks;
using Metamatic.UIElements;
using Metamatic.UIToolKit;
using UnityEngine.UIElements;

namespace Metamatic.Framework
{
    public class CriticalNotification: AsyncVisualElement
    {
        private Label m_Label;
        private int m_Iteration;
        public Observable<(int, string, float)> m_Showing = new Observable<(int, string, float)>();
        
        public CriticalNotification()
        {
            AddToClassList(UIToolKitCore.DarkEnoughClassName);
            
            AddToClassList(UIToolKitCore.Tailwind.Padding24);
            AddToClassList(UIToolKitCore.Tailwind.BackgroundColorPanelTransparent);
            AddToClassList(UIToolKitCore.Tailwind.BorderRadius12);
            AddToClassList(UIToolKitCore.Tailwind.PositionAbsolute);
            m_Label = new Label();
            m_Label.AddToClassList(UIToolKitCore.Tailwind.TextSizeDefault);
            m_Label.AddToClassList(UIToolKitCore.Tailwind.TextColorDefault);
            Add(m_Label);
            style.opacity = 0;
            style.translate = new StyleTranslate(new Translate(-50, -50, 0));
            style.display = DisplayStyle.None;
            style.top = new StyleLength(new Length(50, LengthUnit.Percent));
            style.left = new StyleLength(new Length(50, LengthUnit.Percent));
        }

        public void Show(string message, float duration = 2f)
        {
            unchecked
            {
                m_Iteration++;
            }
            m_Showing.Value = (m_Iteration, message, duration);
        }

        public DisposableAction Show(string message)
        {
            unchecked
            {
                m_Iteration++;
            }
            var thisIteration = m_Iteration;
            m_Showing.Value = (m_Iteration, message, 0f);
            
            return new DisposableAction(() =>
            {
                if (m_Iteration == thisIteration)
                {
                    m_Showing.Value = (m_Iteration, string.Empty, 0f);
                }
            });
        }

        public override async UniTask OnAttachedAsync(CancellationSignal detachSignal)
        {
            using var showing = m_Showing.WatchLifecycle(OnShowChanged);
            // Subscribe to geometry changes
            RegisterCallback<GeometryChangedEvent>(OnGeometryChange);
            await detachSignal;
            RegisterCallback<GeometryChangedEvent>(OnGeometryChange);
        }
        
        private void OnGeometryChange(GeometryChangedEvent evt)
        {
            
        }
        
        private async UniTaskVoid OnShowChanged((int, string, float) showing, CancellationSignal signal)
        {
            var (iteration, message, duration) = showing;
            
            if (string.IsNullOrEmpty(message))
            {
                return;
            }
            m_Label.text = message;
            style.display = DisplayStyle.Flex;
            style.opacity = 0;
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
    }
}