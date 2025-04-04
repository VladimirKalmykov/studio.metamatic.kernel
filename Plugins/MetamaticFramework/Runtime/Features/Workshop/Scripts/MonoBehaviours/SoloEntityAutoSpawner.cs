using System;
using System.Collections.Generic;
using Codice.CM.Common.Tree.Partial;
using Cysharp.Threading.Tasks;
using Metamatic;
using Metamatic.Framework;
using Metamatic.Framework.Workshop;
using Metamatic.SaveSystem;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Console = Metamatic.Console;
using Random = UnityEngine.Random;



[RequireComponent(typeof(Entity))]
[RequireService(typeof(TimeService))]
public class SoloEntityAutoSpawner : AsyncBehaviour, ISavable
{
    [Version("1.0.0")]
    public struct SaveData
    {
        public string spawnedEntityId;
        public float lastDismissedTime;
    }
    
    [SerializeField] private AssetReferenceT<EntityBlueprint> blueprint;
    [Tooltip("If entity is destroyed, it will respawn after this duration.")]
    [SerializeField] private float respawnDuration = 5f;
    [Tooltip("Spawn delay in seconds. Entity will spawn after this duration.")]
    [SerializeField] private float randomSpawnDelay = 0f;

    private Observable<string> m_ObservableSpawnedEntityId = new Observable<string>();
    private float m_LastDismissedTime;

    private TimeService m_TimeService;
    
    protected override async UniTask Run(Ready ready, Signal signal)
    {
        var entity = GetComponent<Entity>();
        m_TimeService = await Service.Resolve<TimeService>(signal);
        using var watchHydration = entity.WatchHydratedAsync(OnEntityHydrated);
        await ready();
    }
    
    /// <summary>
    /// Entity is hydrated. It guarantees that entity load virtual or saved data
    /// </summary>
    /// <param name="dehydrateSignal"></param>
    private async UniTaskVoid OnEntityHydrated(Signal dehydrateSignal)
    {
        /* First, check if entities with ids in m_SpawnedEntities are already spawned. */
        if (String.IsNullOrEmpty(m_ObservableSpawnedEntityId.value))
        {
            Spawn();
        }
        using var observeEntity = m_ObservableSpawnedEntityId.WatchAsync(OnEntityIdChanged, id => !String.IsNullOrEmpty(id));
        await dehydrateSignal;
    }
    
    private async UniTaskVoid OnEntityIdChanged(string id, Signal stopSignal)
    {
        using var existenceTracker = GameSystem.virtualWorld.WatchEntityExistsAsync(id, OnEntityNotExists);
        await stopSignal;
    }

    private async UniTaskVoid OnEntityNotExists(VirtualEntity virtualEntity, bool exists, Signal createdSignal)
    {
        if (!exists)
        {
            if (m_LastDismissedTime == 0f)
            {
                m_LastDismissedTime = m_TimeService.time;
            }
            using var createSchedule = ScheduleCreateEntity();
            await createdSignal;
        }
    }

    private DisposableAction ScheduleCreateEntity()
    {
        return (DisposableAction)DisposableOperation.Call(StartSpawnTimer);
    }
    
    private async UniTask StartSpawnTimer(Signal cancellationSignal)
    {
        try
        {
            var timeDiff = m_TimeService.time - m_LastDismissedTime;

            var delay = (respawnDuration + Random.Range(0, randomSpawnDelay)) - timeDiff;
            if (delay > 0)
            {
                await PlayerLoop.Delay(delay, signal: cancellationSignal);
            }
            m_LastDismissedTime = 0f;
            Spawn();
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
        catch (Exception e)
        {
            Console.Error("SoloEntityAutoSpawner", "StartSpawnTimer", e);
        }
    }

    private void Spawn()
    {
        m_ObservableSpawnedEntityId.uniqValue =
            GameSystem.virtualWorld.CreateEntity(transform.position, blueprint.AssetGUID).id;
    }

    public object Save()
    {
        var data = new SaveData
        {
            spawnedEntityId = m_ObservableSpawnedEntityId.value,
            lastDismissedTime = m_LastDismissedTime
        };
        
        return data;
    }

    public UniTask Load(object data)
    {
        var saveData = (SaveData) data;
        m_ObservableSpawnedEntityId.uniqValue = saveData.spawnedEntityId;
        m_LastDismissedTime = saveData.lastDismissedTime;
        return UniTask.CompletedTask;
    }
}
