using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Metamatic.DevTools;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Metamatic.Framework.Workshop
{
    /// <summary>
    /// Активирует / деактивирует GameArea3D, когда её AABB пересекает frustum главной камеры.
    /// </summary>
    public class GameAreas3dFOVActivatorService : Service
    {
        // ---------------- Публичные настройки --------------------------------

        /// <summary>Запас вокруг frustum в метрах (область считается «видимой», пока не покинет frустум + padding).</summary>
        [SerializeField] private float frustumPadding = 2f;

        /// <summary>Периодичность проверки, секунд.</summary>
        [SerializeField] private float checkInterval = 0.1f;

        // ---- внутренние коллекции (как в 2D-версии) --------------------------

        private static readonly ObservableList<GameArea> s_GameAreas = new();
        private readonly HashSet<string> m_Intersected = new(); // TODO: Replace with simple bool in TrackAreaVisibility
        private readonly Dictionary<string, SignalSource> m_ActivationSignals = new();
        private readonly Dictionary<string, SignalSource> m_DeactivateDebounce = new();

        public  static void RegisterGameArea(GameArea area)   => s_GameAreas.Add(area);
        public  static void UnregisterGameArea(GameArea area) => s_GameAreas.Remove(area);
        
        [ShowInInspector, ReadOnly]
        private bool m_DebugQuickCheck;
        [ShowInInspector, ReadOnly]
        private bool m_DebugPreciseCheck;

        // ---------------------------------------------------------------------

        private CameraService m_CameraService;

        protected override async UniTask RunService(Ready ready, Signal signal)
        {
            m_CameraService = await Service.Resolve<CameraService>(signal);

            using var _areasLifecycle = s_GameAreas.WatchEachLifecycle(OnAreaAnnounce);
            using var monitorQuickCheck = Monitoring.Create("Camera FOV", "Quick check", () => m_DebugQuickCheck);
            using var monitorPreciseCheck = Monitoring.Create("Camera FOV", "Precise check", () => m_DebugPreciseCheck);
            GameArea.OnAreasEnabled  += RegisterGameArea;
            GameArea.OnAreasDisabled += UnregisterGameArea;

            await ready();

            GameArea.OnAreasEnabled  -= RegisterGameArea;
            GameArea.OnAreasDisabled -= UnregisterGameArea;
            s_GameAreas.Clear();
        }

        // =====================================================================
        #region Area ↔ Camera link

        private async UniTaskVoid OnAreaAnnounce(GameArea area, int _, Signal cancel)
        {
            if (area is not GameArea3D area3D) return;

            // ждём/слушаем камеру, потом трекаем видимость
            using var watchCam = m_CameraService.WatchMainCameraAsync(
                (camCtrl, camCancel) =>
                {
                    if (camCtrl == null) return default;
                    return TrackAreaVisibility(area3D, camCtrl.camera, camCancel);
                });

            await cancel;
        }

        /// <summary>
        /// Циклическая проверка видимости конкретной области для конкретной камеры.
        /// </summary>
        private async UniTaskVoid TrackAreaVisibility(GameArea3D area,
                                                  Camera      cam,
                                                  Signal      cancel)
        {
            while (!cancel.IsCancellationRequested)
            {
                // 1. Быстрый отбор — если за спиной или далеко за farClip.
                m_DebugQuickCheck = QuickCheck(area, cam);
                if (!QuickCheck(area, cam))
                {
                    SetVisible(area, false);
                    await Delay();  // skip costly stuff
                    continue;
                }

                // 2. Точный AABB × плоскости frustum
                bool visible = PreciseCheck(area, cam);
                m_DebugPreciseCheck = visible;

                SetVisible(area, visible);

                await Delay();
            }

            // ------------------------------------------------ local func
            UniTask Delay() =>
                PlayerLoop.Delay(TimeSpan.FromSeconds(checkInterval),
                              PlayerLoopTiming.FixedUpdate,
                              cancel);

        }

        #endregion
        // =====================================================================
        #region Geometry checks

        /// <summary>Быстрая проверка «за спиной / за пределами farClip». Очень дёшево.</summary>
        private bool QuickCheck(GameArea3D area, Camera cam)
        {
            var t        = area.transform;
            var worldCtr = t.TransformPoint(area.bounds.center);
            var toCtr    = worldCtr - cam.transform.position;
            var dist     = toCtr.magnitude;

            // позади камеры?
            if (Vector3.Dot(cam.transform.forward, toCtr) <= 0f) return false;

            // слишком далеко (учитываем padding + «радиус» бокса)
            float radius = area.bounds.extents.magnitude;
            if (dist - radius > cam.farClipPlane - frustumPadding) return false;

            // всё остальное оставляем «точной» проверке
            return true;
        }

        /// <summary>Полный AABB × Frustum c padding.</summary>
        private bool PreciseCheck(GameArea3D area, Camera cam)
        {
            // 1) получаем мировой AABB без всяких паддингов
            var t        = area.transform;
            var centerW  = t.TransformPoint(area.bounds.center);
            var sizeW    = Vector3.Scale(area.bounds.size, t.lossyScale);
            var worldAab = new Bounds(centerW, sizeW);

            // 2) получаем плоскости и сдвигаем каждую «наружу» на frustumPadding
            var planes = GeometryUtility.CalculateFrustumPlanes(cam);
            for (int i = 0; i < planes.Length; i++)
            {
                // двигаем плоскость от origin в сторону её нормали на frustumPadding
                planes[i].distance += frustumPadding;
            }

            // 3) обычный тест пересечения
            return GeometryUtility.TestPlanesAABB(planes, worldAab);
        }


        #endregion
        // =====================================================================
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
            catch (OperationCanceledException) { }
            catch (Exception e)               { Console.Error("GameAreas3DService", e); }
        }

        #endregion
        // =====================================================================
        #region TODO / будущие оптимизации

        /*
         * • Burst-джоб под IJobParallelFor, который берёт массив worldAABB + массив плоскостей,
         *   кладёт результаты во NativeArray<bool>. Включаем только если областей ≥ N.
         *
         * • По расстоянию до центра area вычислять LOD-уровень и дёргать area.LoadLod(level).
         *
         * • Система приоритета: если область далеко, проверяем её реже (adaptive checkInterval).
         */

        #endregion
    }
}
