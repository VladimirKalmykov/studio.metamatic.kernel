using Metamatic;
using UnityEngine.UI;

namespace Metamatic
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public abstract class ThemeBase : ScriptableObject
    {
        [SerializeField] public ThemeSchema schema;
        public event Action OnChange;

        public DisposableAction Watch(Action action)
        {
            OnChange += action;
            action?.Invoke();
            return new DisposableAction(() => OnChange -= action);
        }

#if UNITY_EDITOR
        protected virtual void OnValidate() { OnChange?.Invoke(); }
#endif

        // Унифицированные геттеры: контракт есть (schema + key kind), реализация свободная в наследнике
        public abstract bool TryGetColor(in ThemeKeyRef key, out Color color);
        public abstract bool TryGetColorBlock(in ThemeKeyRef key, out ColorBlock block);
    }

}