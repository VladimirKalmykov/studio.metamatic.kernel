using Cysharp.Threading.Tasks;
using Metamatic.UIElements;
using UnityEngine.UIElements;

namespace Metamatic.Framework
{
    public class SideFlyoutPanel: AsyncVisualElement
    {
        private readonly Observable<Position> m_Position = new Observable<Position>();
        private readonly Observable<bool> m_IsOpened = new Observable<bool>();
        private VisualElement m_Content;
        public enum Position
        {
            Top,
            Bottom,
            Left,
            Right
        }
        public override VisualElement contentContainer => m_Content;
        public SideFlyoutPanel(Position position, bool isOpened = false)
        {
            AddToClassList("MetamaticUIKit_SideFlyoutPanel");
            
            m_Position.Value = position;
            m_IsOpened.Value = isOpened;
            
            m_Content = new VisualElement();
            m_Content.AddToClassList("MetamaticUIKit_SideFlyoutPanelContent");
            hierarchy.Add(m_Content);
        }

        public override async UniTask OnAttachedAsync(Signal detachSignal)
        {
            using var watchPosition = m_Position.Watch(OnPositionChanged);
            using var watchOpened = m_IsOpened.Watch(OnOpenedChanged);
            await detachSignal;
        }
        
        private void OnPositionChanged(Position position)
        {
            switch (position)
            {
                case Position.Top:
                    RemoveFromClassList("MetamaticUIKit_SideFlyoutPanel_Bottom");
                    RemoveFromClassList("MetamaticUIKit_SideFlyoutPanel_Left");
                    RemoveFromClassList("MetamaticUIKit_SideFlyoutPanel_Right");
                    AddToClassList("MetamaticUIKit_SideFlyoutPanel_Top");
                    break;
                case Position.Bottom:
                    RemoveFromClassList("MetamaticUIKit_SideFlyoutPanel_Top");
                    RemoveFromClassList("MetamaticUIKit_SideFlyoutPanel_Left");
                    RemoveFromClassList("MetamaticUIKit_SideFlyoutPanel_Right");
                    AddToClassList("MetamaticUIKit_SideFlyoutPanel_Bottom");
                    break;
                case Position.Left:
                    RemoveFromClassList("MetamaticUIKit_SideFlyoutPanel_Top");
                    RemoveFromClassList("MetamaticUIKit_SideFlyoutPanel_Bottom");
                    RemoveFromClassList("MetamaticUIKit_SideFlyoutPanel_Right");
                    AddToClassList("MetamaticUIKit_SideFlyoutPanel_Left");
                    break;
                case Position.Right:
                    RemoveFromClassList("MetamaticUIKit_SideFlyoutPanel_Top");
                    RemoveFromClassList("MetamaticUIKit_SideFlyoutPanel_Bottom");
                    RemoveFromClassList("MetamaticUIKit_SideFlyoutPanel_Left");
                    AddToClassList("MetamaticUIKit_SideFlyoutPanel_Right");
                    break;
            }
        }
        
        private void OnOpenedChanged(bool isOpened)
        {
            if (isOpened)
            {
                AddToClassList("MetamaticUIKit_SideFlyoutPanel_Opened");
            }
            else
            {
                RemoveFromClassList("MetamaticUIKit_SideFlyoutPanel_Opened");
            }
        }
    }
}