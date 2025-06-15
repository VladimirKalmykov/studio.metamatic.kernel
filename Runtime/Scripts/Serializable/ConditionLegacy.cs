// Condition.cs
namespace Metamatic
{
    using System;
    using System.Reflection;
    using UnityEngine;

    [Serializable]
    public class ConditionLegacy
    {
        [SerializeField] private UnityEngine.Object target;
        [SerializeField] private string memberName;
        [SerializeField] private string stringArg;
        [SerializeField] private bool invert;            // ← новое поле

        // кеш для MemberInfo
        private MemberInfo m_CachedMember;
        private UnityEngine.Object m_CachedTarget;

        public bool Evaluate()
        {
            if (target == null || string.IsNullOrEmpty(memberName))
                return true;

            if (m_CachedTarget != target)
                m_CachedMember = null;

            if (m_CachedMember == null)
            {
                var type = target.GetType();
                foreach (var m in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    var ps = m.GetParameters();
                    if (m.ReturnType == typeof(bool) &&
                       (ps.Length == 0 || (ps.Length == 1 && ps[0].ParameterType == typeof(string))) &&
                        m.Name == memberName)
                    {
                        m_CachedMember = m;
                        break;
                    }
                }

                if (m_CachedMember == null)
                {
                    var prop = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (prop != null &&
                        prop.PropertyType == typeof(bool) &&
                        prop.GetGetMethod(true) != null)
                    {
                        m_CachedMember = prop;
                    }
                }

                m_CachedTarget = target;
            }

            bool result = false;
            if (m_CachedMember is MethodInfo mi)
            {
                var ps = mi.GetParameters();
                result = ps.Length == 0
                    ? (bool)mi.Invoke(target, null)
                    : (bool)mi.Invoke(target, new object[] { stringArg });
            }
            else if (m_CachedMember is PropertyInfo pi)
            {
                result = (bool)pi.GetValue(target);
            }
            else
            {
                Debug.LogWarning($"[Condition] `{memberName}` not found on {target}");
            }

            // применение инверсии
            return invert ? !result : result;
        }
    }
}
