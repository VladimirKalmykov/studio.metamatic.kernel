using System;
using Cysharp.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Metamatic.Framework
{
    /// <summary>
    /// Responsible for managing focus spots in the game. Dynamic areas can load its assets in the focus spot.
    /// For example, you'd like to preload a dynamic area when the player is going to enter it. You can create a focus point inside the area and register it here.
    /// Theme GameArea3dFocusSpotActivatorService will activate the area when the focus point is inside the area.
    /// </summary>
    public static class FocusSpotsSystem
    {
        public abstract class FocusSpot
        {
            
        }
        public class PointFocusSpot : FocusSpot
        {
            public Func<Vector3> getter { get; private set; }

            public PointFocusSpot(Func<Vector3> getter)
            {
                this.getter = getter;
            }
            
            public Vector3 GetPosition()
            {
                return getter();
            }
        }
        public class SphereFocusSpot : FocusSpot
        {
            public Func<(Vector3, float)> getter { get; private set; }

            public SphereFocusSpot(Func<(Vector3, float)> getter)
            {
                this.getter = getter;
            }
            
            public (Vector3, float) GetPosition()
            {
                return getter();
            }
        }
        public static readonly ObservableList<FocusSpot> FocusSpots = new ObservableList<FocusSpot>();
        
        public static DisposableAction RegisterFocusSpot(FocusSpot focusSpot)
        {
            return FocusSpots.AddManaged(focusSpot);
        }

        public static DisposableAction WatchFocusSpotsAsync(Func<FocusSpot, Signal, UniTaskVoid> handler)
        {
            return FocusSpots.WatchEachLifecycle(Handler);
            
            UniTaskVoid Handler(FocusSpot focusSpot, int index, Signal signal)
            {
                return handler(focusSpot, signal);
            }
        }
        
        #if UNITY_EDITOR
        private const float POINT_RADIUS = 0.25f;

        // Unity вызовет этот метод для каждого FocusSpotsGizmoAnchor в сцене
        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        private static void DrawFocusSpots(
            SystemGizmoAnchor anchor,
            GizmoType gizmoType)
        {
            // Рисуем любой нужный набор спотов
            foreach (var spot in FocusSpotsSystem.FocusSpots)
            {
                switch (spot)
                {
                    case FocusSpotsSystem.PointFocusSpot p:
                        Gizmos.color = new Color(1f, 0.34f, 0f);
                        Gizmos.DrawSphere(p.GetPosition(), POINT_RADIUS);
                        break;

                    case FocusSpotsSystem.SphereFocusSpot s:
                        var (center, radius) = s.GetPosition();

                        Gizmos.color = new Color(1f, 0.34f, 0f);
                        Gizmos.DrawWireSphere(center, radius);
                        break;
                }
            }
        }
        #endif
    }
}