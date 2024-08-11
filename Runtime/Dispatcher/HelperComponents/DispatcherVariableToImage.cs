using Cysharp.Threading.Tasks;

namespace Metamatic
{
    using System.Threading;
    using Metamatic.Actions;
    using UnityEngine;
    using UnityEngine.UI;

    public class DispatcherVariableToImage : AsyncBehaviour
    {
        public string variableName;
        public Sprite fallbackSprite;

        private Image m_Image;

        protected override async UniTask Run(Ready ready, CancellationToken cancellationToken)
        {
            m_Image = GetComponent<Image>();
            using var listener =
                Dispatcher.global.SubscribeVariable<Sprite>(variableName, OnValueChange, fallbackSprite);
            await ready();
        }

        private void OnValueChange(Sprite value)
        {
            m_Image.sprite = value;
        }
    }

}