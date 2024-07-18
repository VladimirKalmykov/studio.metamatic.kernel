using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace Metamatic
{
    [CustomEditor(typeof(DoubleImage))]
    public class DoubleImageEditor: ImageEditor
    {
        /* Extend ImageEditor */
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            /* Get the target object */
            var doubleImage = target as DoubleImage;
            /* Create a new property field for the first color */
            doubleImage.color = EditorGUILayout.ColorField("First Color", doubleImage.color);
            /* Create a new property field for the second sprite */
            doubleImage.secondSprite = (Sprite) EditorGUILayout.ObjectField("Second Sprite", doubleImage.secondSprite, typeof(Sprite), false);
            /* Create a new property field for the second color */
            doubleImage.secondColor = EditorGUILayout.ColorField("Second Color", doubleImage.secondColor);
        }
    }
}