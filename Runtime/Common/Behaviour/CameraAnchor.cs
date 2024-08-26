using Cysharp.Threading.Tasks;

namespace Metamatic
{
    using System.Threading;
    using UnityEngine;

    public class CameraAnchor : AsyncBehaviour
    {
        [SerializeField] private Camera m_Camera;
        public new Camera camera => m_Camera;

        protected override async UniTask Run(Ready ready, CancellationToken cancellationToken)
        {
            /* Register camera controller */
            using var registration = Dispatcher.global.Register(this);
            await ready();
        }
    }

}