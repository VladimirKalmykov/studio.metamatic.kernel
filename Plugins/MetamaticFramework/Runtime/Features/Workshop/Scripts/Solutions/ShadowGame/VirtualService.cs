using System;
using Cysharp.Threading.Tasks;
using Metamatic;
using Metamatic.Framework;
using Metamatic.SaveSystem;

namespace Metamatic.Framework
{
    public class VirtualService: VirtualBehaviour, ISavable
    {
        public Type serviceType { get; }
        public ShadowWorld shadowWorld { get; }
        private Service m_OriginService;
        private object m_SaveData;
        
        public VirtualService(ShadowWorld shadowWorld, Type serviceType)
        {
            this.serviceType = serviceType;
            this.shadowWorld = shadowWorld;
        }
        
        public override async UniTaskVoid OnAwakeAsync(Signal cancellationSignal)
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
                m_OriginService = service;
                await cancellationSignal;
                m_OriginService = null;
                /* Save service data back to cache */
                m_SaveData = savable.Save();
                enabled = true;
            }
        }

        public object Save()
        {
            if (m_OriginService == null)
            {
                return m_SaveData;
            }
            if (m_OriginService is ISavable savable)
            {
                return savable.Save();
            }
            return null;
        }

        public UniTask Load(object data)
        {
            m_SaveData = data;
            return UniTask.CompletedTask;
        }
    }
}