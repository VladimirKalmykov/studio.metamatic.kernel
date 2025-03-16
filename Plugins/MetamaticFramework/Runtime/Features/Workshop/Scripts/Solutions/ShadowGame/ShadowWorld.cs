using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Metamatic.Framework.Workshop;
using Metamatic.SaveSystem;

namespace Metamatic.Framework
{
    public class ShadowWorld: ISavable, IDisposable
    {
        [Serializable]
        [Version("1.0.0")]
        public class SaveServiceData
        {
            public Type serviceType;
            public Type dataType;
            public object data;
        }

        [Serializable]
        [Version("1.0.0")]
        public struct SaveData
        {
            public string dateTime;
            public SaveServiceData[] services;
        }
        
        private readonly Dictionary<Type, VirtualService> m_Services = new();
        private readonly Dictionary<(Type, string), VirtualBehaviour> m_Entities = new();
        private SignalSource m_DisposeSignalSource;

        public ShadowWorld()
        {
            m_DisposeSignalSource = SignalSource.Create();
            RunAsync(m_DisposeSignalSource.Signal).Forget();
        }
        
        private async UniTaskVoid RunAsync(Signal cancellationSignal)
        {
            using var serviceSubscription = Service.WatchServices(OnServiceRegistered);
            await cancellationSignal;
        }

        private void OnServiceRegistered(Service service)
        {
            /* Create virtual service for real service */
            GetOrCreateVirtualService(typeof(Service));
        }

        public VirtualService GetOrCreateVirtualService(Type type)
        {
            if (!m_Services.TryGetValue(type, out var service))
            {
                service = new VirtualService(this, type);
                service.Awake();
                m_Services.Add(type, service);
            }

            return service;
        }

        public object Save()
        {
            /*
            var servicesReadyTasks = Service.Services.Values.Select(service => service.WhenReady(cancellationSignal)).ToArray();
            await UniTask.WhenAll(servicesReadyTasks);
            */

            var dataList = new List<SaveServiceData>();
            /* Walk services and get its data */
            foreach (var service in m_Services.Values)
            {
                var data = service.Save();
                if (data != null)
                {
                    dataList.Add(new SaveServiceData
                    {
                        serviceType = service.serviceType, // TODO: Проблема обратного преобразования, если сервис переименуется
                        dataType = data.GetType(), // TODO: Рудимент, тип данных сохраняется через конвертер (но нужно проверить)
                        data = data
                    });
                }
            }

            var saveData = new SaveData
            {
                dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                services = dataList.ToArray()
            };

            return saveData;
        }

        public async UniTask Load(object data)
        {
            if (data is SaveData saveData)
            {
                var tasks = new List<UniTask>();
                foreach (var serviceData in saveData.services)
                {
                    if (Service.Services.TryGetValue(serviceData.serviceType, out var service))
                    {
                        if (service is ISavable savable)
                        {
                            tasks.Add(savable.Load(serviceData.data));
                        }
                    }
                    else
                    {
                        Console.Error("Launcher", $"Service not found: {serviceData.serviceType}");
                        GetOrCreateVirtualService(serviceData.serviceType).Load(serviceData.data);
                    }
                }
                await UniTask.WhenAll(tasks);
            }
        }

        public void Dispose()
        {
            // TODO: Maybe use one common signal source for all services?
            /* Dispose all services */
            foreach (var service in m_Services.ToArray())
            {
                var key = service.Key;
                var value = service.Value;
                m_Services.Remove(key);
                value.enabled = false;
                value.Dispose();
            }
        }
    }
}