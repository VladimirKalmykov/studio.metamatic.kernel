using System;
using Cysharp.Threading.Tasks;
using Metamatic.SaveSystem;

namespace Metamatic.Framework
{
    public class VirtualService: VirtualBehaviour, ISavable
    {
        public Type serviceType { get; }
        public VirtualWorld virtualWorld { get; }
        protected Service OriginService;
        private object m_SaveData;
        
        public VirtualService(VirtualWorld virtualWorld, Type serviceType)
        {
            this.serviceType = serviceType;
            this.virtualWorld = virtualWorld;
        }
        
        public async UniTaskVoid OnAwakeAsync(Signal cancellationSignal)
        {
            using var serviceSubscription = Service.WatchServiceAsync(serviceType, OnServiceSpawned);
            await cancellationSignal;
        }
        
        private async UniTaskVoid OnServiceSpawned(Service service, Signal cancellationSignal)
        {
            if (service is ISavable savable)
            {
                /* Load cached service data to real service */
                enabled = false;
                savable.Load(m_SaveData);
                m_SaveData = null;
                OriginService = service;
                await cancellationSignal;
                OriginService = null;
                /* Save service data back to cache */
                m_SaveData = savable.Save();
                enabled = true;
            }
        }

        public object Save()
        {
            if (OriginService is ISavable savable)
            {
                return savable.Save();
            }
            return m_SaveData;
        }

        public UniTask Load(object data)
        {
            if (OriginService is ISavable savable)
            {
                return savable.Load(data);
            }
            m_SaveData = data;
            return UniTask.CompletedTask;
        }
    }
}