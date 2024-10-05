using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;

namespace Metamatic.Framework
{
    using UnityEngine;

    public abstract class Widget: AsyncVisualElement
    {
        #region Static
        public struct Preferences {
            public readonly Vector2 Position;
            public readonly Vector2 Size;
            public readonly Vector2 MinSize;
            public readonly Vector2 MaxSize;
            
            public Preferences(Vector2 position, Vector2 size, Vector2 minSize, Vector2 maxSize)
            {
                this.Position = position;
                this.Size = size;
                this.MinSize = minSize;
                this.MaxSize = maxSize;
            }
        }
        private static List<(Type, MetamaticWidgetAttribute)> WidgetClasses;
        public static List<(Type type, MetamaticWidgetAttribute metadata)> GetWidgetClasses()
        {
            if (WidgetClasses != null) return WidgetClasses;
            WidgetClasses = new List<(Type, MetamaticWidgetAttribute)>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();

                foreach (var type in types)
                {
                    // Проверяем, наследуется ли класс от Widget
                    if (type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(Widget)))
                    {
                        // Проверяем наличие атрибута MetamaticWidgetAttribute
                        var attribute = type.GetCustomAttribute<MetamaticWidgetAttribute>();
                        if (attribute != null)
                        {
                            WidgetClasses.Add((type, attribute));
                        }
                    }
                }
            }

            return WidgetClasses;
        }
        #endregion
        public override UniTask OnAttachedAsync(CancellationSignal detachSignal)
        {
            return OnPanelAttachedAsync(detachSignal);
        }
        
        #region Abstract methods
        public abstract UniTask OnPanelAttachedAsync(CancellationSignal cancellationSignal);
        public abstract Preferences GetDefaultPreferences();
        #endregion
        
        
    }

}