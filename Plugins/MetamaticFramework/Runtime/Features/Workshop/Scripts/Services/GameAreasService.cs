using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Metamatic.MicroBehaviours;

namespace Metamatic.Framework.Workshop
{
    [RequireService(typeof(FOV2DService))]
    public class GameAreasService: Service
    {
        private static readonly ObservableList<GameArea> GameAreas = new();
        private readonly HashSet<GameArea> m_IntersectedAreas = new();
        private readonly Dictionary<GameArea, SignalSource> m_AreaActivationSignals = new();
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
                if (m_IntersectedAreas.Add(gameArea))
                {
                    // Activate area
                    ActivateArea(gameArea);
                }
            }
            else
            {
                if (m_IntersectedAreas.Remove(gameArea))
                {
                    // Deactivate area
                    DeactivateArea(gameArea);
                }
            }
        }
        
        public void ActivateArea(GameArea2D gameArea)
        {
            if (!m_AreaActivationSignals.TryGetValue(gameArea, out var signalSource))
            {
                Console.Debug("GameAreasService", $"Activating area {gameArea.name}");
                signalSource = SignalSource.Create();
                // ...Here's activation
            }
        }
        
        public void DeactivateArea(GameArea2D gameArea)
        {
            Console.Debug("GameAreasService", $"Deactivating area {gameArea.name}");
            if (m_AreaActivationSignals.TryGetValue(gameArea, out var signalSource))
            {
                signalSource.TryRaise();
                m_AreaActivationSignals.Remove(gameArea);
                // ...Here's deactivation
            }
        }
    }
}