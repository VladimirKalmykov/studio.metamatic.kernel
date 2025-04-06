using System;
using Cysharp.Threading.Tasks;
using Metamatic.SaveSystem;

namespace Metamatic.Framework
{
    public class VirtualService: ISavable, ISavablePlace, IDisposable
    {
        public Type serviceType { get; }
        protected Service OriginService;
        private object m_SaveData = null;
        private object m_PlaceSaveData = null;
        private string m_SavedPlaceAddress = null;
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
                if (m_SaveData != null)
                {
                    /* Load cached service data to real service */
                    savable.Load(m_SaveData);
                    m_SaveData = null;
                }
            }

            if (service is ISavablePlace savablePlace)
            {
                if (!String.IsNullOrEmpty(m_SavedPlaceAddress))
                {
                    /* Load cached service data to real service */
                    savablePlace.LoadFromPlace(m_SavedPlaceAddress, m_PlaceSaveData);
                    m_PlaceSaveData = null;
                }
            }
            OriginService = service;
            await cancellationSignal;
            OriginService = null;

            if (service is ISavable savableBack)
            {
                /* Save service data back to cache */
                m_SaveData = savableBack.Save();
            }
            if (service is ISavablePlace savablePlaceBack)
            {
                var currentPlace = GameSystem.GetCurrentPlaceAddress();
                if (!String.IsNullOrEmpty(currentPlace))
                {
                    /* Save service data back to cache */
                    m_SavedPlaceAddress = currentPlace;
                    m_PlaceSaveData = savablePlaceBack.SaveToPlace(currentPlace);
                }
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

        public object SaveToPlace(string placeAddress)
        {
            if (OriginService is ISavablePlace savable)
            {
                return savable.SaveToPlace(placeAddress);
            }
            return placeAddress == m_SavedPlaceAddress ? m_PlaceSaveData : null;
        }

        public UniTask LoadFromPlace(string placeAddress, object data)
        {
            if (OriginService is ISavablePlace savable)
            {
                m_SavedPlaceAddress = null;
                m_PlaceSaveData = null;
                return savable.LoadFromPlace(placeAddress, data);
            }
            m_SavedPlaceAddress = placeAddress;
            m_PlaceSaveData = data;
            return UniTask.CompletedTask;
        }
    }
}