#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Metamatic
{
    /// <summary>
    /// Кастомный инспектор для <see cref="InputActionsService"/>.
    /// Делает три вещи:
    /// 1. Даёт выбрать *.inputactions*-ассет.
    /// 2. Позволяет редактировать список <c>exclusionActionMaps</c>.
    /// 3. Показывает «Primary Action Map» и список всех карт,
    ///    подсвечивая активные зелёным цветом в рантайме.
    /// </summary>
    [CustomEditor(typeof(InputActionsService))]
    public sealed class InputActionsServiceEditor : UnityEditor.Editor
    {
        // ──────────────────────────────────────────────────────────────
        //  Inspector GUI
        // ──────────────────────────────────────────────────────────────

        public override VisualElement CreateInspectorGUI()
        {
            var service = (InputActionsService)target;
            var root    = new VisualElement { style = { marginLeft = 2, marginRight = 2 } };

            // ----- 1. Input Action Asset ----------------------------------------------------------
            var assetProp = serializedObject.FindProperty("m_InputActionsAsset");
            root.Add(new PropertyField(assetProp, "Input Action Asset"));

            root.Add(new Space(4));

            // ----- 2. Exclusion Action Maps (Reorderable List) ------------------------------------
            var exclProp = serializedObject.FindProperty("exclusionActionMaps");
            root.Add(new Label("Exclusion Action Maps") { style = { unityFontStyleAndWeight = FontStyle.Bold } });
            root.Add(new PropertyField(exclProp) { name = "ExclusionList" });

            root.Add(new Space(8));

            // ----- 3. Runtime-info: Primary & All Maps --------------------------------------------
            var primaryLabel = new Label { style = { unityFontStyleAndWeight = FontStyle.Bold } };
            root.Add(primaryLabel);

            var mapsContainer = new VisualElement();
            root.Add(mapsContainer);

            // Style for active map rows
            var activeColor = new StyleColor(new Color(0.20f, 0.85f, 0.25f)); // ярко-зелёный

            // Runtime update ---------------------------------------------------------
            void RefreshRuntimeInfo()
            {
                // a) Primary Action Map
                var primaryName = service != null ? service.currentActionMap : String.Empty;
                primaryLabel.text = $"Primary Action Map : {primaryName}";

                // b) List of all maps
                mapsContainer.Clear();

                var asset = service != null ? service.inputActionAsset : null;
                if (asset == null) return;

                foreach (var map in asset.actionMaps)
                {
                    var row = new Label($"• {map.name}");
                    if (map.enabled)
                        row.style.color = activeColor;

                    mapsContainer.Add(row);
                }
            }

            // Сразу одно обновление, затем — каждую редакторскую итерацию
            RefreshRuntimeInfo();
            EditorApplication.update += RefreshRuntimeInfo;

            // Убираем callback, когда инспектор выгружается
            root.RegisterCallback<DetachFromPanelEvent>(_ =>
                EditorApplication.update -= RefreshRuntimeInfo);

            return root;
        }
    }

    // Простейший спейсер для UI Toolkit
    internal sealed class Space : VisualElement
    {
        public Space(float height) => style.height = height;
    }
}
#endif
