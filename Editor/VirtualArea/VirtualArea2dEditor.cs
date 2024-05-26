using Metamatic;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Metamatic
{
    [CustomEditor(typeof(VirtualArea2d), true)]
    public class VirtualArea2dEditor: OdinEditor
    {
        bool editModeEnabled = false;
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            GUILayout.Space(20);

            editModeEnabled = GUILayout.Toggle(editModeEnabled, "Enable editor", "Button");
        }
        protected virtual void OnSceneGUI()
        {
            var area = (VirtualArea2d)target;
            if (area.points == null)
            {
                return;
            }
            var transform = area.transform;
            /* Draw points with small circles */
            Handles.color = Color.green;
            Handles.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            foreach (var point in area.points)
            {
                Handles.SphereHandleCap(0, point, Quaternion.identity, HandleUtility.GetHandleSize(transform.position) * area.gizmoSize, EventType.Repaint);
            }

            if (editModeEnabled)
            {
                EditorGUI.BeginChangeCheck();
                for (int i = 0; i < area.points.Count; i++)
                {
                    area.points[i] = Handles.FreeMoveHandle(area.points[i], 0.05f, Vector3.one * 0.5f, Handles.RectangleHandleCap);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(area, "Change points");
                    EditorUtility.SetDirty(area);
                }
            }
        }
    }
}