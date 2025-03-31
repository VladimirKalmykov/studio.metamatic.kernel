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
    
    private Observable<Entity> m_ObservableOwnedEntity = new Observable<Entity>();

    private TimeService m_TimeService;
    
    protected override async UniTask Run(Ready ready, Signal signal)
    {
        m_TimeService = await Service.Resolve<TimeService>(signal);
        using var gameWatcher = GameSystem.WatchGameLoadedAsync(OnGameLoaded);
        await ready();
    }

    private async UniTaskVoid OnGameLoaded(Signal stopSignal)
    {
        /* First, check if entities with ids in m_SpawnedEntities are already spawned. */
        if (String.IsNullOrEmpty(m_ObservableSpawnedEntityId.value))
        {
            Spawn();
        }
        using var observeEntity = m_ObservableSpawnedEntityId.WatchAsync(OnEntityIdChanged, id => !String.IsNullOrEmpty(id));
        await stopSignal;
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
            await PlayerLoop.Delay(respawnDuration + Random.Range(0, randomSpawnDelay), signal: cancellationSignal);
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
        return new SaveData
        {
            spawnedEntityId = m_ObservableSpawnedEntityId.value,
            lastDismissedTime = m_LastDismissedTime
        };
    }

    public UniTask Load(object data)
    {
        var saveData = (SaveData) data;
        m_ObservableSpawnedEntityId.uniqValue = saveData.spawnedEntityId;
        m_LastDismissedTime = saveData.lastDismissedTime;
        return UniTask.CompletedTask;
    }
}
