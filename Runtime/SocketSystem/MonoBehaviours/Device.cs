using Cysharp.Threading.Tasks;
using Metamatic;
using UnityEngine;

namespace Metamatic.SocketSystem
{
    [RequireComponent(typeof(Module))]
    public abstract class Device: AsyncBehaviour
    {
        
    }
    public abstract class Device<TData> : Device where TData : struct
    {
        protected readonly ObservableProxy<TData> ObservableData = new ();
        public TData data => ObservableData.value;
        
        public DisposableAction Bind(Observable<TData> oData)
        {
            ObservableData.Bind(oData);

            return DisposableAction.Create(() =>
            {
                ObservableData.Unbind(oData);
            });
        }

        protected override async UniTask Run(Ready ready, Signal signal)
        {
            var module = GetComponent<Module>();
            // Resolve controller
            var controller = module.connectedSlot?.controller;
            using var register = controller != null ? controller.RegisterDevice(this) : DisposableAction.Empty;
            using var watchBlueprint = ObservableData.WatchAsync(OnDataChangeAsync);
            await signal;
        }
        
        protected abstract UniTaskVoid OnDataChangeAsync(TData data, Signal cancellationSignal);
    }
}