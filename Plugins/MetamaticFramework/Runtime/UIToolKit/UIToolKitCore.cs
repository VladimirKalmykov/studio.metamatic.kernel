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