using Metamatic.UIToolKit;
using UnityEditor.UIElements;
using UnityEngine;

namespace Metamatic.Framework
{
    using UnityEngine.UIElements;

    public class GridSettingsBoxGroup : VisualElement
    {
        private Grid2DSettings m_Value;
        
        private Vector2Field m_GridSize;
        private Vector2Field m_CellSize;
        private Vector3Field m_Rotation;
        private Vector3Field m_Offset;
        private ColorField m_Color;
        private EnumField m_Appearance;
        private FloatField m_Weight;
        
        public event System.Action<Grid2DSettings> OnValueChanged;
        public event System.Action OnReset;
        
        public Grid2DSettings value
        {
            get => m_Value;
            set
            {
                m_Value = value;
                m_GridSize.value = value.gridSize;
                m_CellSize.value = value.cellSize;
                m_Rotation.value = value.rotation;
                m_Offset.value = value.offset;
                m_Color.value = value.color;
                m_Appearance.value = value.appearance;
                m_Weight.value = value.weight;
            }
        }
        
        public GridSettingsBoxGroup()
        {
            styleSheets.Add(UIToolKitCore.tailwindStyleSheet);
            styleSheets.Add(UIToolKitCore.styleSheet);
            Add(CreateGridSettings2d());
        }

        public VisualElement CreateGridSettings2d()
        {
            // Grid settings
            var gridSettings = new BoxGroup("Grid", innerPadding: 6);

            // Grid size
            var gridConfigRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginLeft = -6,
                    marginRight = -6,
                    marginBottom = 6,
                    marginTop = 6
                }
            };
            gridSettings.Add(gridConfigRow);

            var Gap = 12;

            // Grid size (Vector2)
            var gridSizeField = new Field("Grid size")
            {
                style =
                {
                    width = 120,
                    paddingLeft = Gap / 2f,
                    paddingRight = Gap / 2f
                }
            };
            var gridSize = m_GridSize = new Vector2Field();
            gridSize.RegisterValueChangedCallback(evt =>
            {
                var newValue = evt.newValue;
                if (evt.newValue.x < 0 || evt.newValue.y < 0)
                {
                    newValue.x = Mathf.Max(0, newValue.x);
                    gridSize.value = Vector2.zero;
                }

                var currentValue = m_Value;
                currentValue.gridSize = newValue;
                m_Value = currentValue;
                OnValueChanged?.Invoke(currentValue);
            });
            gridSizeField.Add(gridSize);
            gridConfigRow.Add(gridSizeField);

            // Cell size
            var cellSizeField = new Field("Cell size")
            {
                style =
                {
                    width = 120,
                    paddingLeft = Gap / 2f,
                    paddingRight = Gap / 2f
                }
            };
            var cellSize = m_CellSize = new Vector2Field();
            cellSizeField.Add(cellSize);
            cellSize.RegisterValueChangedCallback(evt =>
            {
                var newValue = evt.newValue;
                if (evt.newValue.x < 0 || evt.newValue.y < 0)
                {
                    newValue.x = Mathf.Max(0, newValue.x);
                    cellSize.value = Vector2.zero;
                }

                var currentValue = m_Value;
                currentValue.cellSize = newValue;
                m_Value = currentValue;
                OnValueChanged?.Invoke(currentValue);
            });
            gridConfigRow.Add(cellSizeField);


            // Rotation (Quaternion)
            var rotationField = new Field("Rotation")
            {
                style =
                {
                    width = 160,
                    paddingLeft = Gap / 2f,
                    paddingRight = Gap / 2f
                }
            };
            var rotation = m_Rotation = new Vector3Field();
            rotationField.Add(rotation);
            rotation.RegisterValueChangedCallback(evt =>
            {
                var newValue = evt.newValue;

                var currentValue = m_Value;
                currentValue.rotation = newValue;
                m_Value = currentValue;
                OnValueChanged?.Invoke(currentValue);
            });
            gridConfigRow.Add(rotationField);

            var gridConfigRow2 = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginLeft = -3,
                    marginRight = -6,
                    marginBottom = 6,
                    marginTop = 6
                }
            };
            gridSettings.Add(gridConfigRow2);

            // Offset
            var offsetField = new Field("Offset")
            {
                style =
                {
                    width = 160,
                    paddingLeft = Gap / 2f,
                    paddingRight = Gap / 2f
                }
            };
            var offset = m_Offset = new Vector3Field();
            offsetField.Add(offset);
            offset.RegisterValueChangedCallback(evt =>
            {
                var newValue = evt.newValue;

                var currentValue = m_Value;
                currentValue.offset = newValue;
                m_Value = currentValue;
                OnValueChanged?.Invoke(currentValue);
            });
            gridConfigRow2.Add(offsetField);

            // Color
            var colorField = new Field("Color")
            {
                style =
                {
                    width = 80,
                    paddingLeft = Gap / 2f,
                    paddingRight = Gap / 2f
                }
            };
            var color = m_Color = new ColorField();
            colorField.Add(color);
            color.RegisterValueChangedCallback(evt =>
            {
                var newValue = evt.newValue;
                if (evt.newValue.a < 0)
                {
                    newValue.a = Mathf.Max(0, newValue.a);
                    color.value = Color.clear;
                }

                var currentValue = m_Value;
                currentValue.color = newValue;
                m_Value = currentValue;
                OnValueChanged?.Invoke(currentValue);
            });
            gridConfigRow2.Add(colorField);
            
            // Appearance
            var appearanceField = new Field("Appearance")
            {
                style =
                {
                    width = 120,
                    paddingLeft = Gap / 2f,
                    paddingRight = Gap / 2f
                }
            };
            var appearance = m_Appearance = new EnumField();
            appearanceField.Add(appearance);
            appearance.RegisterValueChangedCallback(evt =>
            {
                var newValue = (Grid2DSettings.AppearanceType) evt.newValue;

                var currentValue = m_Value;
                currentValue.appearance = newValue;
                m_Value = currentValue;
                OnValueChanged?.Invoke(currentValue);
            });
            appearance.Init(Grid2DSettings.AppearanceType.Lines);
            gridConfigRow2.Add(appearanceField);
            
            // Weight
            var weightField = new Field("Weight")
            {
                style =
                {
                    width = 80,
                    paddingLeft = Gap / 2f,
                    paddingRight = Gap / 2f
                }
            };
            var weight = m_Weight = new FloatField();
            weightField.Add(weight);
            weight.RegisterValueChangedCallback(evt =>
            {
                var newValue = evt.newValue;
                if (evt.newValue < 0)
                {
                    newValue = Mathf.Max(0, newValue);
                    weight.value = 0;
                }

                var currentValue = m_Value;
                currentValue.weight = newValue;
                m_Value = currentValue;
                OnValueChanged?.Invoke(currentValue);
            });
            weightField.Add(weight);
            gridConfigRow2.Add(weightField);
            
            // Add a button to reset the grid settings to default
            var resetButton = new Button(() =>
            {
                OnReset?.Invoke();
            })
            {
                text = "Reset",
                style =
                {
                    width = 80,
                    paddingLeft = Gap / 2f,
                    paddingRight = Gap / 2f
                }
            };
            gridConfigRow2.Add(resetButton);


            return gridSettings;
        }
    }
}