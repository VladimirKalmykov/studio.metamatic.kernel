using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Metamatic.Framework.Workshop
{
    [AddComponentMenu("2D Object/Metamatic/Workshop/Game Area 2D")]
    public class GameArea2D : GameArea
    {
        [SerializeField] public Bounds bounds;

#if UNITY_EDITOR
        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        static void DrawGameAreaGizmos(GameArea2D area, GizmoType gizmoType)
        {
            Transform t = area.transform;
            // Локальные координаты центра + позиция объекта = мировой центр
            Vector2 worldCenter = t.position + area.bounds.center;
            Vector2 size = area.bounds.size;
            Color boundsColor = new Color(0.44f, 0.67f, 0.53f);

            // Рисуем границы через Gizmos – они будут видны всегда
            Gizmos.color = boundsColor;
            Gizmos.DrawWireCube(worldCenter, size);

            // Рисуем название зоны с помощью Handles (можно использовать в OnDrawGizmos)
            GUIStyle labelStyle = new GUIStyle();
            labelStyle.fontSize = 20;
            labelStyle.normal.textColor = boundsColor;
            labelStyle.alignment = TextAnchor.MiddleCenter;
            Handles.Label(worldCenter, area.name, labelStyle);
        }
#endif
    }
}