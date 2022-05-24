using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Metamatic.DevTools
{
    public class BoxGizmo : Gizmo
    {
        public Rect rect;
        
        
    
        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Handles.color = color;
            var rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Handles.matrix = rotationMatrix;
            Handles.DrawDottedLines(new []
            {
                (Vector3)(rect.position + new Vector2(rect.width / 2, -rect.height / 2f)),
                (Vector3)(rect.position + new Vector2(rect.width / 2, rect.height / 2f)),
                (Vector3)(rect.position + new Vector2(rect.width / 2, rect.height / 2f)),
                (Vector3)(rect.position + new Vector2(-rect.width / 2, rect.height / 2f)),
                (Vector3)(rect.position + new Vector2(-rect.width / 2, rect.height / 2f)),
                (Vector3)(rect.position + new Vector2(-rect.width / 2, -rect.height / 2f)),
                (Vector3)(rect.position + new Vector2(-rect.width / 2, -rect.height / 2f)),
                (Vector3)(rect.position + new Vector2(rect.width / 2, -rect.height / 2f)),
            }, 0.3f);
            Handles.DrawWireDisc(rect.position, Vector3.forward, 0.3f);
            base.OnDrawGizmos();
        }

        private void OnDrawGizmosSelected()
        {
            Handles.color = color;
            var rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Handles.matrix = rotationMatrix;
            var labelStyle = new GUIStyle();
            labelStyle.fontSize = 9;
            labelStyle.normal.textColor = color;
            labelStyle.alignment = TextAnchor.LowerRight;
            Handles.Label(rect.position + new Vector2(rect.width / 2, -rect.height / 2f) + new Vector2(0.3f, 0.3f) , title, labelStyle);
        }
#endif
    }
}