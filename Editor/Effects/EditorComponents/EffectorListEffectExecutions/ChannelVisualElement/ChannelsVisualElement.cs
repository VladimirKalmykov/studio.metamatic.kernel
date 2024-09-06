using UnityEngine.UIElements;

namespace Metamatic.Effects
{
    public class ChannelsVisualElement : VisualElement
    {
        public EffectorEdtitor editor { get; private set; }
        public EffectorLegacy effector { get; private set; }
        public ChannelsVisualElement(EffectorEdtitor editor) : base()
        {
            this.viewDataKey = "Channels";
            this.editor = editor;
            effector = (EffectorLegacy) this.editor.target;
            effector.editor_SubscribeChannels(HandleChannelsChange);
            AddToClassList("ChannelsList");

            
        }
        
        ~ChannelsVisualElement()
        {
            effector.editor_UnsubscribeChannels(HandleChannelsChange);
        }

        private void HandleChannelsChange(EffectsChannel[] channels)
        {
            Clear();
            foreach (var channel in channels)
            {
                Add(new ChannelVisualElement(channel));
            }
        }

        public void OnEnable()
        {
            for (var i = 0; i < this.hierarchy.childCount; i++)
            {
                var child = this.hierarchy.ElementAt(i) as ChannelVisualElement;
                child?.OnEnable();
            }
        }

        public void OnDisable()
        {
            for (var i = 0; i < this.hierarchy.childCount; i++)
            {
                var child = this.hierarchy.ElementAt(i) as ChannelVisualElement;
                child?.OnDisable();
            }

            effector.editor_UnsubscribeChannels(HandleChannelsChange);
        }
    }
}