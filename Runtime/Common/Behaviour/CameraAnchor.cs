using Cysharp.Threading.Tasks;

namespace Metamatic
{
    using System.Threading;
    using UnityEngine;
    using Metamatic.Actions;

    public class CameraAnchor : AsyncBehaviourBase
    {
        [SerializeField] private Camera m_Camera;
        public Camera camera => m_Camera;

        protected override async UniTaskVoid RunAsync(CancellationToken cancellationToken)
        {
            /* Register camera controller */
            using var registration = Dispatcher.global.Register(this);
            await cancellationToken;
        }
    }

}