using System;
using Cysharp.Threading.Tasks;
using Metamatic.SaveSystem;

namespace Metamatic.Framework
{
    public class VirtualService: ISavable, IDisposable
    {
        public Type serviceType { get; }
        protected Service OriginService;
        private object m_SaveData;
        private event Action OnDispose;
        
        public VirtualService( Type serviceType)
        {
            this.serviceType = serviceType;
            OnDispose += Service.WatchServiceAsync(serviceType, OnServiceSpawned);
        }
        
        private async UniTaskVoid OnServiceSpawned(Service service, Signal cancellationSignal)
        {
            if (service is ISavable savable)
            {
                /* Load cached service data to real service */
                savable.Load(m_SaveData);
                m_SaveData = null;
                OriginService = service;
                await cancellationSignal;
                OriginService = null;
                /* Save service data back to cache */
                m_SaveData = savable.Save();
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

        public void Dispose()
        {
            OnDispose?.Invoke();
            OnDispose = null;
        }
    }
}