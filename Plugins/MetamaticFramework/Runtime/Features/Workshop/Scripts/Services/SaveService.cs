using System;
using System.Collections.Generic;
using System.Globalization;
using Cysharp.Threading.Tasks;
using Metamatic;
using Metamatic.Framework;
using Metamatic.SaveSystem;
using UnityEngine;
using Console = Metamatic.Console;

namespace Metamatic.Framework.Workshop
{
    public class SaveService: Service
    {
        [Version("1.0.0")]
        public struct GameSaveData
        {
            public ServiceSaveData[] Services;
            public string DateTime;
            public string Place;
            public VirtualWorld.SaveData World;
        }
        protected override UniTask RunService(Ready ready, Signal signal)
        {
            return ready();
        }

        public async UniTask<bool> Save(string address, Signal cancellationSignal)
        {
            if (!GameSystem.isGameRunning || VirtualWorld.Current == null)
            {
                Console.Warning("SaveService", "Save() called but no world is loaded.");
                return false;
            }
            using var pending = PendingSystem.CreatePending("Save game");
            var worldSaveData = (VirtualWorld.SaveData)VirtualWorld.Current.SaveToWorld();
            
            var serviceSaveData = new List<ServiceSaveData>();
            foreach (var (type, service) in Service.Services)
            {
                if (service is IGlobalSavable globalSavable)
                {
                    serviceSaveData.Add(new ServiceSaveData
                    {
                        serviceType = service.GetType(),
                        data = globalSavable.SaveToGlobal()
                    });
                }    
            }
            
            var saveData = new GameSaveData
            {
                Services = serviceSaveData.ToArray(),
                World = worldSaveData,
                DateTime = DateTime.Now.ToString(CultureInfo.CurrentCulture),
                Place = GameSystem.GetCurrentPlaceAddress(),
            };
            
            await Repository.LocalStorage.Set(address, saveData, cancellationSignal);
            return true;
        }
        
        public async UniTask<SaveService.GameSaveData> Load(string loadAddress = "latest", bool seamless = false, Signal cancellationSignal = default)
        {
            var data = await Repository.LocalStorage.Get<GameSaveData>(loadAddress, cancellationSignal);
            
            // Load services
            foreach (var serviceSaveData in data.Services)
            {
                if (Service.Services.TryGetValue(serviceSaveData.serviceType, out var service))
                {
                    if (service is IGlobalSavable globalSavable)
                    {
                        await globalSavable.LoadFromGlobal(serviceSaveData.data);
                    }
                }
                else
                {
                    Console.Error("SaveService", $"Service {serviceSaveData.serviceType} not found.");
                    throw new Exception($"Service {serviceSaveData.serviceType} not found.");
                }
            }

            return data;
        }
    }
}