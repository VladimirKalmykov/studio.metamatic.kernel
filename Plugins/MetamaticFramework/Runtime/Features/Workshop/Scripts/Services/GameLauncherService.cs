using System;
using Metamatic.SaveSystem;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Metamatic.Framework.Workshop
{
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using Metamatic;
    using Newtonsoft.Json;

    /// <summary>
    /// Launcher is responsible for saving, loading the game, and start game from the beginning.
    /// </summary>
    public class GameLauncherService : Service
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
        
        #region Fields
        [SerializeField] private AssetReference m_MainMenuScene;
        [SerializeField] private AssetReference m_GameplayScene;
        #endregion

        protected override UniTask RunService(Ready ready, Signal signal)
        {
            return ready();
        }

        public async UniTask Save(string address = "latest", Signal cancellationSignal = default)
        {
            var servicesReadyTasks = Service.Services.Values.Select(service => service.WhenReady(cancellationSignal)).ToArray();
            await UniTask.WhenAll(servicesReadyTasks);

            var dataList = new List<SaveServiceData>();
            /* Walk services and get its data */
            foreach (var service in Service.Services.Values)
            {
                if (service is ISavable savable)
                {
                    var data = savable.Save();
                    if (data != null)
                    {
                        dataList.Add(new SaveServiceData
                        {
                            serviceType = service.GetType(),
                            dataType = data.GetType(),
                            data = data
                        });
                    }
                }
            }

            var saveData = new SaveData
            {
                dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                services = dataList.ToArray()
            };

            Console.Debug("Launcher", $"Saving game data: {saveData}");
            await Repository.LocalStorage.Set(address, saveData, cancellationSignal);
        }

        public async UniTask Load(string saveAddress = "latest", Signal signal = default)
        {
            var saveData = await Repository.LocalStorage.Get<SaveData>(saveAddress, signal);
            Console.Debug("Launcher", $"Loaded game data: {saveData}");
            
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
                }
            }
        }

        /// <summary>
        /// Start the game from the beginning.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async UniTask LaunchFromStretch(Signal cancellationSignal = default) // Chapter chapter
        {
            var servicesReadyTasks = Service.Services.Values.Select(service => service.WhenReady(cancellationSignal)).ToArray();
            await UniTask.WhenAll(servicesReadyTasks);
            
            var tasks = new List<UniTask>();
            foreach (var service in Service.Services.Values)
            {
                if (service is ISavable savable)
                {
                    tasks.Add(savable.Bootstrap());
                }
            }
            
            await UniTask.WhenAll(tasks);
        }
    }
}