using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metamatic.UIToolKit
{
    public static class UIToolKitCore
    {
        public static class Tailwind
        {
            /* Flex direction */
            public const string Row = "MetamaticTailwind_Row";
            public const string Column = "MetamaticTailwind_Column";
            
            /* Grow */
            public const string Grow0 = "MetamaticTailwind_Grow0";
            public const string Grow1 = "MetamaticTailwind_Grow1";
            public const string Grow2 = "MetamaticTailwind_Grow2";
            public const string Grow3 = "MetamaticTailwind_Grow3";
            
            /* Justify content */
            public const string JustifyStart = "MetamaticTailwind_JustifyContentStart";
            public const string JustifyEnd = "MetamaticTailwind_JustifyContentEnd";
            public const string JustifyCenter = "MetamaticTailwind_JustifyContentCenter";
            public const string JustifyBetween = "MetamaticTailwind_JustifyContentBetween";
            public const string JustifyAround = "MetamaticTailwind_JustifyContentAround";
            
            /* Align items */
            public const string AlignStart = "MetamaticTailwind_AlignItemsStart";
            public const string AlignEnd = "MetamaticTailwind_AlignItemsEnd";
            public const string AlignCenter = "MetamaticTailwind_AlignItemsCenter";
            public const string AlignBaseline = "MetamaticTailwind_AlignItemsBaseline";
            public const string AlignStretch = "MetamaticTailwind_AlignItemsStretch";
            
            /* Padding */
            public const string Padding0 = "MetamaticTailwind_Padding0";
            public const string Padding6 = "MetamaticTailwind_Padding6";
            public const string Padding12 = "MetamaticTailwind_Padding12";
            public const string Padding24 = "MetamaticTailwind_Padding24";
            
            /* Right padding */
            public const string PaddingRight0 = "MetamaticTailwind_PaddingRight0";
            public const string PaddingRight6 = "MetamaticTailwind_PaddingRight6";
            public const string PaddingRight12 = "MetamaticTailwind_PaddingRight12";
            public const string PaddingRight24 = "MetamaticTailwind_PaddingRight24";
            
            /* Padding left */
            public const string PaddingLeft0 = "MetamaticTailwind_PaddingLeft0";
            public const string PaddingLeft6 = "MetamaticTailwind_PaddingLeft6";
            public const string PaddingLeft12 = "MetamaticTailwind_PaddingLeft12";
            public const string PaddingLeft24 = "MetamaticTailwind_PaddingLeft24";
            
            /* Margin */
            public const string Margin0 = "MetamaticTailwind_Margin0";
            public const string Margin6 = "MetamaticTailwind_Margin6";
            public const string Margin12 = "MetamaticTailwind_Margin12";
            public const string Margin24 = "MetamaticTailwind_Margin24";
            
            /* Margin right */
            public const string MarginRight0 = "MetamaticTailwind_MarginRight0";
            public const string MarginRight6 = "MetamaticTailwind_MarginRight6";
            public const string MarginRight12 = "MetamaticTailwind_MarginRight12";
            public const string MarginRight24 = "MetamaticTailwind_MarginRight24";
            
            /* Margin left */
            public const string MarginLeft0 = "MetamaticTailwind_MarginLeft0";
            public const string MarginLeft6 = "MetamaticTailwind_MarginLeft6";
            public const string MarginLeft12 = "MetamaticTailwind_MarginLeft12";
            public const string MarginLeft24 = "MetamaticTailwind_MarginLeft24";
            
            /* Fixed width */
            public const string FixedWidth60 = "MetamaticTailwind_FixedWidth60";
            public const string FixedWidth80 = "MetamaticTailwind_FixedWidth80";
            public const string FixedWidth100 = "MetamaticTailwind_FixedWidth100";
            public const string FixedWidth120 = "MetamaticTailwind_FixedWidth120";
            public const string FixedWidth140 = "MetamaticTailwind_FixedWidth140";
            public const string FixedWidth160 = "MetamaticTailwind_FixedWidth160";
            public const string FixedWidth180 = "MetamaticTailwind_FixedWidth180";
            public const string FixedWidth200 = "MetamaticTailwind_FixedWidth200";
            
            /* Background color */
            public const string BackgroundColorDefault = "MetamaticTailwind_BackgroundColorDefault";
            public const string BackgroundColorPanel = "MetamaticTailwind_BackgroundColorPanel";
            public const string BackgroundColorIndicatorInactive = "MetamaticTailwind_BackgroundColorIndicatorInactive";
            public const string BackgroundColorIndicatorActive = "MetamaticTailwind_BackgroundColorIndicatorActive";
            
            /* Border radius */
            public const string BorderRadius0 = "MetamaticTailwind_BorderRadius0";
            public const string BorderRadius3 = "MetamaticTailwind_BorderRadius3";
            public const string BorderRadius4 = "MetamaticTailwind_BorderRadius4";
            public const string BorderRadius8 = "MetamaticTailwind_BorderRadius8";
            public const string BorderRadius12 = "MetamaticTailwind_BorderRadius12";
        }
        #region StyleSheet
        private static StyleSheet _StyleSheet;
        public static StyleSheet styleSheet
        {
            get
            {
                if (_StyleSheet == null)
                {
                    _StyleSheet = Resources.Load<StyleSheet>("Metamatic/UIToolKit/Uss/Styles");
                }

                return _StyleSheet;
            }
        }
        
        private static StyleSheet _TailwindStyleSheet;
        public static StyleSheet tailwindStyleSheet
        {
            get
            {
                if (_TailwindStyleSheet == null)
                {
                    _TailwindStyleSheet = Resources.Load<StyleSheet>("Metamatic/UIToolKit/Uss/Tailwind");
                }

                return _TailwindStyleSheet;
            }
        }
        #endregion
        
        #region Icons cache
        private static readonly Dictionary<string, Texture2D> IconsCache = new Dictionary<string, Texture2D>();
        #endregion
        
        #region Icon paths
        #region 12x12
        private const string Close12X12IconPath = "Metamatic/UIToolKit/Icons/12x12/Close_12x12";
        #endregion
        
        #region 16x16
        private const string CirclePlus16X16IconPath = "Metamatic/UIToolKit/Icons/16x16/CirclePlus_16x16";
        private const string CircleClose16X16IconPath = "Metamatic/UIToolKit/Icons/16x16/CircleClose_16x16";
        private const string Close16X16IconPath = "Metamatic/UIToolKit/Icons/16x16/Close_16x16";
        #endregion
        #endregion
        
        #region Icon getters
        #region 12x12
        public static Texture2D closeIcon12X12 => GetTextureFromResources(Close12X12IconPath);
        #endregion
        
        #region 16x16
        public static Texture2D circlePlusIcon16X16 => GetTextureFromResources(CirclePlus16X16IconPath);
        public static Texture2D circleCloseIcon16X16 => GetTextureFromResources(CircleClose16X16IconPath);
        public static Texture2D closeIcon16X16 => GetTextureFromResources(Close16X16IconPath);
        #endregion
        #endregion 
        
        #region ClassNames
        public const string DarkEnoughClassName = "MetamaticUIKit_DarkEnough";
        public const string PaddedContentClassName = "MetamaticUIKit_PaddedContent";
        /// <summary>
        /// Creates vertical layout with 6px visual gap between and around children
        /// </summary>
        public const string VerticalLayoutClassName = "MetamaticUIKit_VerticalLayout";
        #endregion
        
        private static Texture2D GetTextureFromResources(string path)
        {
            if (!IconsCache.TryGetValue(path, out var texture))
            {
                texture = Resources.Load<Texture2D>(path);
                IconsCache.Add(path, texture);
            }

            return texture;
        }
        
        // Метод для анализа и создания словаря
        public static Dictionary<string, List<(string, Func<Texture2D>)>> GetAllIcons()
        {
            // Получаем все статические свойства класса UIKit, тип которых Texture2D
            var iconProperties = typeof(UIToolKitCore).GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.PropertyType == typeof(Texture2D));

            // Регулярное выражение для анализа имени свойства
            var iconRegex = new Regex(@"(?<name>.+)Icon(?<size>\d+[Xx]\d+)", RegexOptions.Compiled);

            // Словарь, где ключ - это размер иконки, а значение - список кортежей (имя иконки, делегат получения иконки)
            var iconDictionary = new Dictionary<string, List<(string, Func<Texture2D>)>>();

            // Проходим по всем найденным свойствам
            foreach (var property in iconProperties)
            {
                var match = iconRegex.Match(property.Name);
                if (match.Success)
                {
                    // Извлекаем имя иконки и размер
                    var iconName = match.Groups["name"].Value;
                    var iconSize = match.Groups["size"].Value;

                    // Создаем Func<Texture2D> для получения иконки
                    Func<Texture2D> iconGetter = () => (Texture2D)property.GetValue(null);

                    // Если словарь еще не содержит этот размер, добавляем его
                    if (!iconDictionary.ContainsKey(iconSize))
                    {
                        iconDictionary[iconSize] = new List<(string, Func<Texture2D>)>();
                    }

                    // Добавляем в список кортежей для данного размера иконку
                    iconDictionary[iconSize].Add((iconName, iconGetter));
                }
            }

            return iconDictionary;
        }
    }
}