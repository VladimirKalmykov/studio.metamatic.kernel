using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Metamatic.MicroBehaviours;

namespace Metamatic.Framework.Workshop
{
    [RequireService(typeof(FOV2DService))]
    public class GameAreasService: Service
    {
        private static readonly ObservableList<GameArea> GameAreas = new();
        private readonly HashSet<string> m_IntersectedAreas = new();
        private readonly Dictionary<string, SignalSource> m_AreaActivationSignals = new();
        private readonly Dictionary<string, SignalSource> m_DeactivateDebounceSignals = new();
        public static void RegisterGameArea(GameArea gameArea)
        {
            GameAreas.Add(gameArea);
        }
        public static void UnregisterGameArea(GameArea gameArea)
        {
            GameAreas.Remove(gameArea);
        }
        private FOV2DService m_Fov2dService;
        protected override async UniTask RunService(Ready ready, Signal signal)
        {
            m_Fov2dService = await Service.Resolve<FOV2DService>(signal);
            using var observeGameAreas = GameAreas.WatchEachLifecycle(OnArea2dAnnounce);
            await ready();
        }

        private async UniTaskVoid OnArea2dAnnounce(GameArea gameArea, int index, Signal cancellationSignal)
        {
            
            if (gameArea is GameArea2D area2d)
            {
                Console.Debug("GameAreasService", $"Area 2D {gameArea.name} announced");
                using (m_Fov2dService.TrackArea(area2d, CameraFOVTracker2d.FovBoundsType.Full, OnAreaFOVIntersection))
                {
                    await cancellationSignal;
                    Console.Debug("GameAreasService", $"Area 2D {gameArea.name} released");
                }
            }
        }
        
        private void OnAreaFOVIntersection(bool intersected, CameraFOVTracker2d.AreaIntersection intersection)
        {
            var gameArea = intersection.area as GameArea2D;
            if (gameArea == null)
            {
                return;
            }

            if (intersected)
            {
                if (m_IntersectedAreas.Add(gameArea.id))
                {
                    // Activate area
                    ActivateArea(gameArea);
                }
            }
            else
            {
                if (m_IntersectedAreas.Remove(gameArea.id))
                {
                    // Deactivate area
                    DeactivateArea(gameArea);
                }
            }
        }
        
        public void ActivateArea(GameArea2D gameArea)
        {
            if (!m_AreaActivationSignals.TryGetValue(gameArea.id, out var signalSource))
            {
                Console.Debug("GameAreasService", $"Activating area {gameArea.name}");
                signalSource = SignalSource.Create();
                // ...Here's activation
                m_AreaActivationSignals.Add(gameArea.id, signalSource);
                HandleTackErrors(gameArea.Activate(signalSource.Signal)).Forget();
            }
            else if (m_DeactivateDebounceSignals.TryGetValue(gameArea.id, out var debounceSignalSource))
            {
                debounceSignalSource.TryRaise();
                m_DeactivateDebounceSignals.Remove(gameArea.id);
            }
        }
        
        public void DeactivateArea(GameArea2D gameArea)
        {
            Console.Debug("GameAreasService", $"Deactivating area {gameArea.name}");
            if (m_AreaActivationSignals.TryGetValue(gameArea.id, out var signalSource))
            {
                if (!m_DeactivateDebounceSignals.TryGetValue(gameArea.id, out var debounceSignalSource))
                {
                    debounceSignalSource = SignalSource.Create();
                    m_DeactivateDebounceSignals.Add(gameArea.id, debounceSignalSource);
                    DebouncedDeactivateScene(gameArea.id, signalSource, debounceSignalSource).Forget();
                }
            }
        }
        
        private async UniTaskVoid DebouncedDeactivateScene(string gameAreaId, SignalSource deactivateSignal, SignalSource cancellationSignalSource)
        {
            try
            {
                await PlayerLoop.Delay(TimeSpan.FromSeconds(3), signal: cancellationSignalSource.Signal);
                deactivateSignal.TryRaise();
                m_AreaActivationSignals.Remove(gameAreaId);
                cancellationSignalSource.Dispose();
                m_DeactivateDebounceSignals.Remove(gameAreaId);
            }
            catch (OperationCanceledException)
            {
                // Skip
            } catch (Exception e)
            {
                Console.Error("GameAreasService", e);
            }
        }

        private async UniTaskVoid HandleTackErrors(UniTask task)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                // Skip
            }
            catch (Exception e)
            {
                Console.Error("GameAreasService", e);
            }
        }
    }
}