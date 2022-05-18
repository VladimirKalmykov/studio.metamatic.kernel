using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic.Effects
{
    public sealed class ChannelEffectExecutionVisualElement : TextElement
    {
        public readonly EffectExecutionContext execution;
        public ChannelEffectExecutionVisualElement(EffectExecutionContext execution)
        {
            this.execution = execution;
            AddToClassList("Execution");
            text = execution.ToString();
        }

        public async void Release(Action<ChannelEffectExecutionVisualElement> onComplete)
        {
            AddToClassList("Released");
            await Task.Delay(2000);
            onComplete(this);
        }
    }
}