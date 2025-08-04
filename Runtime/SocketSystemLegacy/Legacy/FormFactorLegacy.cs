using System;
using System.Collections.Generic;
using UnityEngine;

namespace Metamatic
{
    public readonly struct FormFactorSocketSchemeInfo
    {
        public readonly Type type;
        public readonly bool optional;
        public FormFactorSocketSchemeInfo(Type type, bool optional)
        {
            this.type = type;
            this.optional = optional;
        }
    }
    public readonly struct FormFactorSocketInfo
    {
        public readonly Type type;
        public readonly bool optional;
        public FormFactorSocketInfo(Type type, bool optional)
        {
            this.type = type;
            this.optional = optional;
        }
    }
    public readonly struct FormFactorPlugInfo
    {
        public readonly Type type;
        public readonly bool optional;
        public FormFactorPlugInfo(Type type, bool optional)
        {
            this.type = type;
            this.optional = optional;
        }
    }
    public class FormFactorLegacy
    {
        /// <summary>
        /// Get all RequireSocket attributes for the class and all its base classes
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static RequireSocket[] GetRequireSocketAttributes(Type type)
        {
            List<RequireSocket> requireSockets = new List<RequireSocket>();
            Debug.Assert(type.IsSubclassOf(typeof(FormFactorLegacy)), "RequireSocket attribute can be used only for FormFactor subclasses");
            var attributes = type.GetCustomAttributes(typeof(RequireSocket), true);
            foreach (var attr in attributes)
            {
                // Обработка атрибута
                RequireSocket requireSocket = attr as RequireSocket;
                requireSockets.Add(requireSocket);
                // Ваш код здесь
            }
            return requireSockets.ToArray();
        }
        private static readonly Dictionary<Type, FormFactorLegacy> SocketSchemesInstances = new Dictionary<Type, FormFactorLegacy>();
        private Type[] m_CachedSocketTypes;
        [Obsolete("Out of concept", true)]
        public static T Pool<T>() where T: FormFactorLegacy, new()
        {
            Type type = typeof(T);
            if (SocketSchemesInstances.TryGetValue(type, out var instantiate))
            {
                return instantiate as T;
            }
            instantiate = new T();
            SocketSchemesInstances.Add(type, instantiate);
            return (T) instantiate;
        }
        /// <summary>
        /// Get socket types for the form factor
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static FormFactorSocketSchemeInfo[] GetSocketSchemeTypes(Type socketSchemeType)
        {
            var requireSocketAttributes = GetRequireSocketAttributes(socketSchemeType);
            List<FormFactorSocketSchemeInfo> socketSchemeTypes = new List<FormFactorSocketSchemeInfo>();
            foreach (var requireSocketAttribute in requireSocketAttributes)
            {
                foreach (var type in requireSocketAttribute.types)
                {
                    socketSchemeTypes.Add(new FormFactorSocketSchemeInfo(type, requireSocketAttribute.optional));
                }
            }
            return socketSchemeTypes.ToArray();
        }
        
        /// <summary>
        /// Get socket types for this form factor
        /// </summary>
        /// <returns></returns>
        public static FormFactorSocketInfo[] GetSocketTypes(Type socketSchemeType)
        {
            var schemeTypes = GetSocketSchemeTypes(socketSchemeType);
            FormFactorSocketInfo[] socketTypes = new FormFactorSocketInfo[schemeTypes.Length];
            for (int i = 0; i < schemeTypes.Length; i++)
            {
                socketTypes[i] = new FormFactorSocketInfo(typeof(SocketLegacy<>).MakeGenericType(schemeTypes[i].type), schemeTypes[i].optional);
            }

            return socketTypes;
        }
        
        public static FormFactorPlugInfo[] GetPlugTypes(Type socketSchemeType)
        {
            var schemeTypes = GetSocketSchemeTypes(socketSchemeType);
            FormFactorPlugInfo[] plugTypes = new FormFactorPlugInfo[schemeTypes.Length];
            for (int i = 0; i < schemeTypes.Length; i++)
            {
                plugTypes[i] = new FormFactorPlugInfo(typeof(PlugLegacy<>).MakeGenericType(schemeTypes[i].type), schemeTypes[i].optional);
            }

            return plugTypes;
        }
    }
    
    public class AnyFormFactor: FormFactorLegacy
    {
        
    }
    
    public class D3FormFactorLegacy: FormFactorLegacy
    {
        
    }
    
    public class D2FormFactor: FormFactorLegacy
    {
        
    }
    
    public class UIFormFactor: FormFactorLegacy
    {
        
    }
}