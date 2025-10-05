using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine.Localization;

namespace Metamatic.SocketSystem
{
    public abstract class ModuleBlueprint: PrefabBlueprint, IFormFactorCompatibility
    {
        public LocalizedString description;
        [ShowInInspector][ReadOnly]
        public abstract Type formFactor { get; }
    }
    
    public abstract class ModuleBlueprint<TData, TConfig>: ModuleBlueprint where TData: struct where TConfig : struct
    {
        public abstract TData BuildData(TConfig config);

        /// <summary>
        /// Mount the module to the target host with the provided observable data.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="oConfig"></param>
        public AbortableAsyncOperation Mount(Host target, Observable<TConfig> oConfig)
        {
            return new AbortableAsyncOperation((cancellation) => InternalMount(target, oConfig.Transform(BuildData), cancellation));
        }
        
        private async UniTask InternalMount(Host target, Observable<TData> oData, AsyncCancellation cancellation)
        {
            try
            {
                using var summon = target.Mount(this);
                var module = await summon.Wait(cancellation.Signal);
                var device = module.gameObject.GetOrAddComponent<Device<TData>>();
                using var bind = device.Bind(oData);
                await cancellation.Signal;
            } catch (OperationCanceledException)
            {
                // Handle cancellation if needed
            }
        }
        
        /*
        public Device<TData> EngageDevice(GameObject gameObject, TConfig config)
        {
            var data = BuildData(config);
            return EngageDevice(gameObject, data);
        }
        
        public Device<TData> EngageDevice(GameObject gameObject, TData data)
        {
            var device = gameObject.AddComponent<Device<TData>>();
            if (device == null)
            {
                throw new InvalidOperationException($"Failed to add Device<{typeof(TData).Name}> component to {gameObject.name}");
            }
            device.SetData(data);
            return device;
        }
        
        */
    }
}