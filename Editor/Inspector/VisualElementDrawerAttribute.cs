using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Metamatic.Inspector
{
   [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class VisualElementDrawerAttribute : Attribute
{
    private static Dictionary<Type, Type> s_DrawerCache;
    private static bool s_IsCacheInitialized = false;
    private static readonly object s_CacheLock = new object();

    public static Type GetDrawer(Type targetType)
    {
        if (!s_IsCacheInitialized)
        {
            lock (s_CacheLock)
            {
                if (!s_IsCacheInitialized)
                {
                    BuildDrawerCache();
                    s_IsCacheInitialized = true;
                }
            }
        }

        if (s_DrawerCache.TryGetValue(targetType, out var bestMatch))
        {
            return bestMatch;
        }

        return null;
    }

    private static void BuildDrawerCache()
    {
        s_DrawerCache = new Dictionary<Type, Type>();

        // Список сборок, в которых будем искать
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        // Получаем все типы, наследующиеся от DrawerVisualElement
        var drawerTypes = assemblies.SelectMany(assembly =>
        {
            try
            {
                return assembly.GetTypes()
                    .Where(type => typeof(DrawerVisualElement).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface);
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null && typeof(DrawerVisualElement).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);
            }
        });

        foreach (var type in drawerTypes)
        {
            var attributes = type.GetCustomAttributes(typeof(VisualElementDrawerAttribute), true)
                .Cast<VisualElementDrawerAttribute>();

            foreach (var attribute in attributes)
            {
                // Если в кэше уже есть этот targetType, сравниваем приоритеты
                if (s_DrawerCache.TryGetValue(attribute.m_Type, out var existingType))
                {
                    var existingAttribute = existingType.GetCustomAttributes(typeof(VisualElementDrawerAttribute), true)
                        .Cast<VisualElementDrawerAttribute>()
                        .First(attr => attr.m_Type == attribute.m_Type);

                    if (attribute.m_Priority > existingAttribute.m_Priority)
                    {
                        s_DrawerCache[attribute.m_Type] = type;
                    }
                }
                else
                {
                    s_DrawerCache[attribute.m_Type] = type;
                }
            }
        }
    }

    // Остальная часть вашего кода
    private readonly Type m_Type;
    private readonly int m_Priority;
    public VisualElementDrawerAttribute(Type type, int priority = 0)
    {
        m_Type = type;
        m_Priority = priority;
    }
}

}