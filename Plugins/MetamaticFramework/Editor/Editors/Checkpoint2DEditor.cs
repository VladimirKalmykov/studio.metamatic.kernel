using Cysharp.Threading.Tasks;
using Metamatic.EditorTools;
using Metamatic.UIToolKit;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Metamatic.Framework.Workshop
{
    [CustomEditor(typeof(Checkpoint))]
    public class Checkpoint2DEditor: AsyncOdinEditor
    {
        private Button m_ToggleEditButton;
        private Checkpoint checkpoint => target as Checkpoint;
        protected override VisualElement CreateInspectorGUIAsync()
        {
            var superRoot = new VisualElement();
            superRoot.AddToClassList("Root");
            // Connect stylesheets
            superRoot.styleSheets.Add(UIToolKitCore.tailwindStyleSheet);
            superRoot.styleSheets.Add(UIToolKitCore.styleSheet);

            VisualElement ugui = new IMGUIContainer(() =>
            {
                if (target != null)
                {
                    OnInspectorGUI();
                }
            });

            superRoot.Add(ugui);
            superRoot.Add(CreateControlsElement());

            Console.Debug("Editor", "Create GUI");

            return superRoot;
        }
        
        private VisualElement CreateControlsElement()
        {
            m_ToggleEditButton = new Button()
            {
                text = "Edit"
            };
            // Listen to click
            m_ToggleEditButton.clicked += () =>
            {
                checkpoint.EditorOnlyEditMode.Value = !checkpoint.EditorOnlyEditMode.Value;
            };
            if (Application.isPlaying)
            {
                m_ToggleEditButton.style.display = DisplayStyle.None;
            }

            return m_ToggleEditButton;
        }

        protected override async UniTask OnEnableAsync(Signal cancellationSignal)
        {
            await WhenInspectorGUIIsCreated();
            using var previewModeListener = checkpoint.EditorOnlyEditMode.Watch(OnEditModeChangeAsync);
            await cancellationSignal;
        }
        
        private void OnEditModeChangeAsync(bool editMode)
        {
            if (editMode)
            {
                m_ToggleEditButton.AddToClassList("MetamaticTailwind_BackgroundColorIndicatorActive");
                m_ToggleEditButton.AddToClassList("MetamaticTailwind_TextColorInverse");
            }
            else
            {
                m_ToggleEditButton.RemoveFromClassList("MetamaticTailwind_BackgroundColorIndicatorActive");
                m_ToggleEditButton.RemoveFromClassList("MetamaticTailwind_TextColorInverse");
            }
        }

        private void OnSceneGUI()
        {
            if (checkpoint == null)
                return;
            if (!checkpoint.EditorOnlyEditMode.Value)
                return;

            // Преобразуем относительную позицию в мировую.
            Vector3 worldSpawnPosition = checkpoint.transform.position + checkpoint.spawnPosition;

            // Настраиваем цвет хэндла.
            Handles.color = Color.green;

            // Начинаем отслеживание изменений.
            EditorGUI.BeginChangeCheck();

            // Вычисляем размер хэндла с учётом масштаба сцены.
            float handleSize = HandleUtility.GetHandleSize(worldSpawnPosition) * 0.1f;

            // Создаем свободно перемещаемый квадратный хэндл.
            Vector3 newWorldPosition = Handles.FreeMoveHandle(
                worldSpawnPosition,
                handleSize,
                Vector3.zero,
                Handles.RectangleHandleCap);

            // Если позиция изменилась, обновляем spawnPosition.
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(checkpoint, "Move Spawn Position");
                // Обновляем относительную позицию, вычитая позицию объекта.
                checkpoint.spawnPosition = newWorldPosition - checkpoint.transform.position;
                EditorUtility.SetDirty(checkpoint);
            }
        }
    }
}