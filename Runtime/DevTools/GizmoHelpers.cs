#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Metamatic.DevTools
{
    public class GizmoHelpers
    {
        public static void DrawOrientation(Transform transform, Vector3 worldAnchorPosition, Quaternion worldAnchorRotation, float arrowSize = 1f, Color color = default)
        {
#if UNITY_EDITOR
            var handleSize = HandleUtility.GetHandleSize(transform.position);
            var radius = 0.2f * handleSize;

            Gizmos.color = color;
            Handles.color = color;

            // Масштабирование плоскости в соответствии с заданным размером
            Handles.DrawWireDisc(worldAnchorPosition, worldAnchorRotation * Vector3.forward, radius);
            
            DrawArrow(transform, worldAnchorPosition, worldAnchorRotation * Vector3.forward * arrowSize, color);
            Gizmos.DrawRay(worldAnchorPosition, worldAnchorRotation * Vector3.up * radius);
            Gizmos.DrawRay(worldAnchorPosition, worldAnchorRotation * Vector3.right * radius);
#endif
        }
        
        public static void DrawArrow(Transform transform, Vector3 position, Vector3 direction, Color color, float arrowHeadLength = 0.15f, float arrowHeadAngle = 20.0f)
        {
            var handleSize = HandleUtility.GetHandleSize(transform.position);
            Gizmos.color = color;
            Gizmos.DrawRay(position, direction * handleSize);

            // Рисуем конус в качестве наконечника стрелки
            Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0,180+arrowHeadAngle,0) * new Vector3(0,0,1) * handleSize;
            Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0,180-arrowHeadAngle,0) * new Vector3(0,0,1) * handleSize;
            Vector3 up = Quaternion.LookRotation(direction) * Quaternion.Euler(180+arrowHeadAngle,0,0) * new Vector3(0,0,1) * handleSize;
            Vector3 down = Quaternion.LookRotation(direction) * Quaternion.Euler(180-arrowHeadAngle,0,0) * new Vector3(0,0,1) * handleSize;

            Gizmos.DrawRay(position + direction * handleSize, right * arrowHeadLength);
            Gizmos.DrawRay(position + direction * handleSize, left * arrowHeadLength);
            Gizmos.DrawRay(position + direction * handleSize, up * arrowHeadLength);
            Gizmos.DrawRay(position + direction * handleSize, down * arrowHeadLength);
        }
    }
}