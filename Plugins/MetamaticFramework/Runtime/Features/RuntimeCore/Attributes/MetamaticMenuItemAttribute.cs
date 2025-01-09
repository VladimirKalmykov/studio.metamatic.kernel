using System;
using System.Collections.Generic;
using System.Reflection;

namespace Metamatic.Framework
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property)]
    public class MetamaticMenuItemAttribute: Attribute
    {
        private static List<MenuItemConfig> _MenuItems;
        
        public readonly string menuName;
        public readonly int sortOrder;

        public MetamaticMenuItemAttribute(string menuName, int sortOrder = 0)
        {
            this.menuName = menuName;
            this.sortOrder = sortOrder;
        }
        
        public static List<MenuItemConfig> GetMenuItemMethods()
        {
            if (_MenuItems != null)
            {
                return _MenuItems;
            }

            _MenuItems = new List<MenuItemConfig>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();

                foreach (var type in types)
                {
                    // Получаем все методы
                    var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    foreach (var method in methods)
                    {
                        var attribute = method.GetCustomAttribute<MetamaticMenuItemAttribute>();
                        if (attribute != null)
                        {
                            void Handler() => method.Invoke(null, null);
                            _MenuItems.Add(new MenuItemConfig(attribute.menuName, action: Handler, sortOrder: attribute.sortOrder));
                        }
                    }

                    // Получаем все поля
                    var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    foreach (var field in fields)
                    {
                        var attribute = field.GetCustomAttribute<MetamaticMenuItemAttribute>();
                        if (attribute != null && IsObservableBool(field.FieldType))
                        {
                            void Handler() => ToggleObservableField(field);
                            Observable<bool> observable = (Observable<bool>)field.GetValue(null);
                            _MenuItems.Add(new MenuItemConfig(attribute.menuName, action: Handler, sortOrder: attribute.sortOrder, boolState: observable));
                        }
                    }

                    // Получаем все свойства
                    var properties = type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    foreach (var property in properties)
                    {
                        var attribute = property.GetCustomAttribute<MetamaticMenuItemAttribute>();
                        if (attribute != null)
                        {
                            if (IsObservableBool(property.PropertyType))
                            {
                                void Handler() => ToggleObservableProperty(property);
                                Observable<bool> observable = (Observable<bool>)property.GetValue(null);
                                _MenuItems.Add(new MenuItemConfig(attribute.menuName, action: Handler, sortOrder: attribute.sortOrder, boolState: observable));
                            }
                        }
                    }
                }
            }

            return _MenuItems;
        }

        // Проверка на тип Observable<bool>
        private static bool IsObservableBool(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Observable<>) && type.GetGenericArguments()[0] == typeof(bool);
        }

        // Тогглинг значений для полей
        private static void ToggleObservableField(FieldInfo field)
        {
            if (field.FieldType == typeof(bool))
            {
                var currentValue = (bool)field.GetValue(null);
                field.SetValue(null, !currentValue);
            }
            else if (IsObservableBool(field.FieldType))
            {
                Observable<bool> observable = (Observable<bool>)field.GetValue(null);
                observable.value = !observable.value;
            }
        }
        
        private static void ToggleBoolField(FieldInfo field)
        {
            var currentValue = (bool)field.GetValue(null);
            field.SetValue(null, !currentValue);
        }

        // Тогглинг значений для свойств
        private static void ToggleObservableProperty(PropertyInfo property)
        {
            if (property.PropertyType == typeof(bool))
            {
                var currentValue = (bool)property.GetValue(null);
                property.SetValue(null, !currentValue);
            }
            else if (IsObservableBool(property.PropertyType))
            {
                Observable<bool> observable = (Observable<bool>)property.GetValue(null);
                observable.value = !observable.value;
            }
        }

        private static void ToggleBoolProperty(PropertyInfo property)
        {
            var currentValue = (bool)property.GetValue(null);
            property.SetValue(null, !currentValue);
        }
    }
}
