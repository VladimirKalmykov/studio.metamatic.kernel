namespace Metamatic
{
    using Sirenix.OdinInspector.Editor;
    using Sirenix.Utilities.Editor;
    using UnityEditor;
    using UnityEngine;

    public class SortingLayerAttributeDrawer : OdinAttributeDrawer<SortingLayerAttribute, int>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var layers = SortingLayer.layers;
            string[] layerNames = new string[layers.Length];
            int[] layerIDs = new int[layers.Length];

            for (int i = 0; i < layers.Length; i++)
            {
                layerNames[i] = layers[i].name;
                layerIDs[i] = layers[i].id;
            }

            int currentLayerIndex = System.Array.IndexOf(layerIDs, this.ValueEntry.SmartValue);
            if (currentLayerIndex == -1) currentLayerIndex = 0;

            int selectedIndex = EditorGUILayout.Popup(label, currentLayerIndex, layerNames);

            ValueEntry.SmartValue = layerIDs[selectedIndex];
        }
    }
}