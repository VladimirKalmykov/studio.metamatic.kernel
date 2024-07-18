namespace Metamatic
{
    using System.Threading;
    using UnityEngine;
    using Metamatic.Actions;

    public class CameraAnchor : AsyncBehaviour
    {
        [SerializeField] private Camera m_Camera;
        public Camera camera => m_Camera;

        protected override async void RunAsync(CancellationToken cancellationToken)
        {
            /* Register camera controller */
            using var registration = Dispatcher.global.Register(this);
            await cancellationToken;
        }
    }

}