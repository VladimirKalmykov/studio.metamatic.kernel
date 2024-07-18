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


        protected override async void RunAsync(CancellationToken cancellationToken)
        {
            m_Image = GetComponent<Image>();
            using var listener =
                Dispatcher.global.SubscribeVariable<Sprite>(variableName, OnValueChange, fallbackSprite);
            await cancellationToken;
        }

        private void OnValueChange(Sprite value)
        {
            m_Image.sprite = value;
        }
    }

}