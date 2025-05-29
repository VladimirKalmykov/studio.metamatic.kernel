using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Metamatic.Framework;

namespace Metamatic.Editor
{
    /// <summary>
    /// Custom inspector with UIElements for convenient editing of m_AssetsToPreload dictionary.
    /// Handles dynamic rebinding so fields remain visible after adding/removing items.
    /// </summary>
    [CustomEditor(typeof(GameObjectFactoryPreloaderService))]
    public class GameObjectFactoryPreloaderServiceEditor : UnityEditor.Editor
    {
        private const float k_IntFieldWidth = 60f;   // width of int field
        private const float k_RowSpacing    = 2f;    // vertical space between rows

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement
            {
                style = { flexDirection = FlexDirection.Column, marginTop = 2, marginLeft = 2, marginRight = 2 }
            };

            var so = serializedObject;
            var dictProp   = so.FindProperty("m_AssetsToPreload");
            var keysProp   = dictProp.FindPropertyRelative("keys");
            var valuesProp = dictProp.FindPropertyRelative("values");

            // Container that will host all rows
            var listContainer = new VisualElement
            {
                style = { flexDirection = FlexDirection.Column }
            };
            root.Add(listContainer);

            // Build initial list
            RebuildList(listContainer, keysProp, valuesProp, so);

            // Add button under the list
            var addButton = new Button(() =>
            {
                so.Update();
                keysProp.arraySize++;
                valuesProp.arraySize++;
                so.ApplyModifiedProperties();
                RebuildList(listContainer, keysProp, valuesProp, so);
            })
            {
                text = "Добавить"
            };
            addButton.style.marginTop = 4;
            addButton.style.alignSelf = Align.FlexStart;
            root.Add(addButton);

            // Bind root so first build fields are connected
            root.Bind(so);
            return root;
        }

        private static void RebuildList(VisualElement container, SerializedProperty keysProp, SerializedProperty valuesProp, SerializedObject so)
        {
            container.Clear();
            so.Update(); // keep properties fresh

            for (int i = 0; i < keysProp.arraySize; i++)
            {
                int index = i; // capture for lambda

                var row = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        alignItems    = Align.Center,
                        marginBottom  = k_RowSpacing
                    }
                };

                // Key field (AssetReference)
                var keyProp  = keysProp.GetArrayElementAtIndex(index);
                var keyField = new PropertyField(keyProp)
                {
                    name = $"KeyField_{index}"
                };
                keyField.style.flexGrow = 1;
                row.Add(keyField);

                // Value field (int)
                var valueProp  = valuesProp.GetArrayElementAtIndex(index);
                var countField = new PropertyField(valueProp)
                {
                    name = $"CountField_{index}"
                };
                countField.style.width      = k_IntFieldWidth;
                countField.style.marginLeft = 4;
                row.Add(countField);

                // Delete button
                var deleteButton = new Button(() =>
                {
                    so.Update();
                    keysProp.DeleteArrayElementAtIndex(index);
                    valuesProp.DeleteArrayElementAtIndex(index);
                    so.ApplyModifiedProperties();
                    RebuildList(container, keysProp, valuesProp, so);
                })
                {
                    tooltip = "Удалить элемент",
                    text    = "✖"
                };
                deleteButton.style.width       = 20;
                deleteButton.style.height      = 18;
                deleteButton.style.marginLeft  = 4;
                row.Add(deleteButton);

                container.Add(row);
            }

            // Bind newly created hierarchy so the PropertyFields are hooked up
            container.Bind(so);
        }
    }
}
