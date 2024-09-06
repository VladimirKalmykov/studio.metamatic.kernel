using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Metamatic.Effects
{
    public class ChannelVisualElement : VisualElement
    {
        private VisualElement m_ItemsContainer;
        public readonly EffectsChannel channel;

        private List<ChannelEffectExecutionVisualElement> m_ChannelEffectExecutionVisualElements =
            new List<ChannelEffectExecutionVisualElement>();
        public ChannelVisualElement(EffectsChannel channel) : base()
        {
            if (channel == null)
            {
                return;
            }

            this.channel = channel;
            AddToClassList("ChannelItem");
            var title = new Label(channel.ToString());
            title.AddToClassList("Label");
            Add(title);
            m_ItemsContainer = new VisualElement();
            m_ItemsContainer.AddToClassList("Items");
            Add(m_ItemsContainer);
            
            channel.editor_SubscribeEffectExecutionsChange(HandleEffectsExecutionsChange);
        }
        
        ~ChannelVisualElement() {
            channel.editor_SubscribeEffectExecutionsChange(HandleEffectsExecutionsChange);
        }

        private void HandleEffectsExecutionsChange(IEffectLegacy.EffectExecutionContext[] executions)
        {
            var onlyNewExecutions = executions.ToList();
            foreach (var item in m_ChannelEffectExecutionVisualElements.ToArray())
            {
                var existenElement = onlyNewExecutions.FirstOrDefault(l => l == item.execution);
                if (existenElement == null)
                {
                    m_ChannelEffectExecutionVisualElements.Remove(item);
                    item.Release(RemoveReleasedExecution);
                }
                else
                {
                    onlyNewExecutions.Remove(existenElement);
                }
            }
            
            foreach (var execution in onlyNewExecutions)
            {
                var item = new ChannelEffectExecutionVisualElement(execution);
                m_ChannelEffectExecutionVisualElements.Add(item);
                m_ItemsContainer.Add(item);
            }
        }

        private void RemoveReleasedExecution(ChannelEffectExecutionVisualElement item)
        {
            m_ItemsContainer.Remove(item);
        }


        public void OnEnable()
        {
            
        }

        public void OnDisable()
        {
            channel.editor_SubscribeEffectExecutionsChange(HandleEffectsExecutionsChange);
        }
    }
}