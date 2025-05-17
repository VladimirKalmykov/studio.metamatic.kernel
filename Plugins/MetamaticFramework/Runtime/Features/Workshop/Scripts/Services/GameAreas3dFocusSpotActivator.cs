using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Metamatic;
using Metamatic.Framework;
using Metamatic.Framework.Workshop;
using UnityEngine;
using Console = Metamatic.Console;

public class GameAreas3dFocusSpotActivator : Service
{
    private static readonly ObservableList<GameArea> s_GameAreas = new();
    public static void RegisterGameArea(GameArea area) => s_GameAreas.Add(area);
    public static void UnregisterGameArea(GameArea area) => s_GameAreas.Remove(area);

    private readonly HashSet<string> m_Intersected = new();
    private readonly Dictionary<string, SignalSource> m_ActivationSignals = new();
    private readonly Dictionary<string, SignalSource> m_DeactivateDebounce = new();

    protected override async UniTask RunService(Ready ready, Signal signal)
    {
        using var _areasLifecycle = s_GameAreas.WatchEachLifecycle(OnAreaAnnounce);
        using var focusSpotsLifecycle = FocusSpotsSystem.WatchFocusSpotsAsync(OnFocusSpotAnnounce);
        GameArea.OnAreasEnabled += RegisterGameArea;
        GameArea.OnAreasDisabled += UnregisterGameArea;

        await ready();

        GameArea.OnAreasEnabled -= RegisterGameArea;
        GameArea.OnAreasDisabled -= UnregisterGameArea;
        s_GameAreas.Clear();
    }

    private async UniTaskVoid OnAreaAnnounce(GameArea area, int _, Signal cancel)
    {
        if (area is not GameArea3D area3D) return;


        await cancel;
    }

    private UniTaskVoid OnFocusSpotAnnounce(FocusSpotsSystem.FocusSpot focusSpot, Signal cancellationSignal)
    {
        switch (focusSpot)
        {
            case FocusSpotsSystem.PointFocusSpot pointFocusSpot:
                HandlePointFocusSpot(pointFocusSpot, cancellationSignal).Forget();
                break;
            case FocusSpotsSystem.SphereFocusSpot sphereFocusSpot:
                HandleSphereFocusSpot(sphereFocusSpot, cancellationSignal).Forget();
                break;
        }
        
        return default;
    }

    private async UniTaskVoid HandlePointFocusSpot(FocusSpotsSystem.PointFocusSpot focusSpot, Signal cancellationSignal)
    {
        using var checkLoop = PlayerLoop.LoopDelay(0.1f, () =>
        {
            foreach (var area in s_GameAreas)
            {
                if (area is GameArea3D area3D)
                {
                    var position = focusSpot.GetPosition();
                    if (area3D.IsIntersected(position))
                    {
                        SetVisible(area3D, true);
                    }
                    else
                    {
                        SetVisible(area3D, false);
                    }
                }
            }
        }, PlayerLoopTiming.FixedUpdate);
        await cancellationSignal;
    }
    
    private async UniTaskVoid HandleSphereFocusSpot(FocusSpotsSystem.SphereFocusSpot focusSpot, Signal cancellationSignal)
    {
        using var checkLoop = PlayerLoop.LoopDelay(0.1f, () =>
        {
            foreach (var area in s_GameAreas)
            {
                if (area is GameArea3D area3D)
                {
                    var (position, radius) = focusSpot.GetPosition();
                    if (area3D.IsIntersectedWithSphere(position, radius))
                    {
                        SetVisible(area3D, true);
                    }
                    else
                    {
                        SetVisible(area3D, false);
                    }
                }
            }
        }, PlayerLoopTiming.FixedUpdate);
        await cancellationSignal;
    }

    #region Activation / Deactivation (копия 2D-логики)

    private void SetVisible(GameArea3D area, bool visible)
    {
        if (visible)
        {
            if (m_Intersected.Add(area.id))
            {
                ActivateArea(area);
            }
        }
        else
        {
            if (m_Intersected.Remove(area.id))
            {
                DeactivateArea(area);
            }
        }
    }

    public void ActivateArea(GameArea3D gameArea)
    {
        if (!m_ActivationSignals.TryGetValue(gameArea.id, out var signalSource))
        {
            Console.Debug("GameAreasService", $"Activating area {gameArea.name}");
            signalSource = SignalSource.Create();
            // ...Here's activation
            m_ActivationSignals.Add(gameArea.id, signalSource);
            gameArea.Activate(signalSource.Signal);
        }
        else if (m_DeactivateDebounce.TryGetValue(gameArea.id, out var debounceSignalSource))
        {
            debounceSignalSource.TryRaise();
            m_DeactivateDebounce.Remove(gameArea.id);
        }
    }

    public void DeactivateArea(GameArea3D gameArea)
    {
        Console.Debug("GameAreasService", $"Deactivating area {gameArea.name}");
        if (m_ActivationSignals.TryGetValue(gameArea.id, out var signalSource))
        {
            if (!m_DeactivateDebounce.TryGetValue(gameArea.id, out var debounceSignalSource))
            {
                debounceSignalSource = SignalSource.Create();
                m_DeactivateDebounce.Add(gameArea.id, debounceSignalSource);
                DebouncedDeactivate(gameArea.id, signalSource, debounceSignalSource).Forget();
            }
        }
    }

    private async UniTaskVoid DebouncedDeactivate(string id,
        SignalSource deactivateSignal,
        SignalSource cancelDebounce)
    {
        try
        {
            // «0» = на следующем кадре, можно увеличить для мягкой delay
            await PlayerLoop.Delay(TimeSpan.FromSeconds(0),
                signal: cancelDebounce.Signal);
            deactivateSignal.Dispose();
            m_DeactivateDebounce.Remove(id);
            if (m_ActivationSignals.Remove(id, out var activateSignalSource))
            {
                activateSignalSource.TryRaise();
            }

            cancelDebounce.Dispose();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            Console.Error("GameAreas3DService", e);
        }
    }

    #endregion
}