using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine.UIElements;
#endif

namespace Metamatic.Framework.Workshop
{
    [AddComponentMenu("3D Object/Metamatic/Workshop/Game Area 3D")]
    public class GameArea3D : GameArea
    {
        [SerializeField] public Bounds bounds;

        // === Runtime ========================================================

        public override bool IsIntersected(Vector3 position)
        {
            // Трансформируем точку в локальное пространство и проверяем Bounds
            Vector3 local = transform.InverseTransformPoint(position);
            return bounds.size != Vector3.zero && bounds.Contains(local);
        }
        
        public bool IsIntersectedWithSphere(Vector3 position, float radiusWorld)
        {
            if (bounds.size == Vector3.zero) return false;

            // 1. Центр сферы → локальное пространство
            Vector3 local = transform.InverseTransformPoint(position);

            // 2. Квадрат расстояния от центра до ящика в локальных координатах
            float sqDist = bounds.SqrDistance(local);   // 0, если внутри

            // 3. Перевод радиуса в локальные единицы.
            //    Берём МАКСИМАЛЬНУЮ компоненту масштаба – это безопасно,
            //    хотя при сильной неравномерности даст лишние срабатывания.
            float maxScale = Mathf.Max(
                Mathf.Abs(transform.lossyScale.x),
                Mathf.Abs(transform.lossyScale.y),
                Mathf.Abs(transform.lossyScale.z));

            float radiusLocal = radiusWorld / maxScale;

            return sqDist <= radiusLocal * radiusLocal;
        }

        // === Editor-helpers ==================================================
#if UNITY_EDITOR
        public Observable<bool> EditorOnlyEditMode = new(false);

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        private static void DrawGizmos(GameArea3D area, GizmoType gizmoType)
        {
            Transform t          = area.transform;
            Vector3   worldCenter = t.TransformPoint(area.bounds.center);
            Vector3   size        = Vector3.Scale(area.bounds.size, t.lossyScale);

            Color c = area.isActive ? new (0.34f, 0f, 0f) : new(0.44f, 0.67f, 0.53f);
            Gizmos.color = c;
            Gizmos.DrawWireCube(worldCenter, size);

            // подпись
            var style = new GUIStyle
            {
                fontSize  = 20,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = c }
            };
            UnityEditor.Handles.Label(worldCenter, area.name, style);
        }
#endif
    }
}