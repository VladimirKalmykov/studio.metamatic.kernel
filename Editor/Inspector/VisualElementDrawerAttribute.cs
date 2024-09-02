using System;
using System.Linq;

namespace Metamatic.Inspector
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class VisualElementDrawerAttribute: Attribute
    {
        public static Type GetDrawer(Type targetType)
        {
            Type bestMatch = null;
            int highestPriority = int.MinValue;

            // Получаем все типы в текущей сборке или в других сборках, если нужно
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes());

            foreach (var type in types)
            {
                // Проверяем наличие атрибута на типе
                var attributes = type.GetCustomAttributes(typeof(VisualElementDrawerAttribute), true)
                    .Cast<VisualElementDrawerAttribute>();

                foreach (var attribute in attributes)
                {
                    // Проверяем, что тип, указанный в атрибуте, совпадает с переданным targetType
                    if (attribute.m_Type == targetType)
                    {
                        // Проверяем, что класс, на котором установлен атрибут, наследуется от DrawerVisualElement
                        if (typeof(DrawerVisualElement).IsAssignableFrom(type))
                        {
                            // Сравниваем приоритеты
                            if (attribute.m_Priority > highestPriority)
                            {
                                highestPriority = attribute.m_Priority;
                                bestMatch = type;  // Возвращаем тип класса, на котором установлен атрибут
                            }
                        }
                    }
                }
            }

            return bestMatch;
        }
        private readonly Type m_Type;
        private readonly int m_Priority;
        public VisualElementDrawerAttribute(Type type, int priority = 0): base()
        {
            m_Type = type;
            m_Priority = priority;
        }
    }
}