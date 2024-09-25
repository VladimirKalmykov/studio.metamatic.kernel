using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Metamatic;
using Metamatic.Framework.CursorTools;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Metamatic.Framework
{
    /// <summary>
    /// Allows to add magic woods to the scene.
    /// </summary>
    public class RuntimeCore
    {
        #region Classes and structs

        public class CustomFunction
        {
            public string Name;
            public Action Action;
            
            public CustomFunction(string name, Action action)
            {
                this.Name = name;
                this.Action = action;
            }
            
            public void Invoke()
            {
                Action.Invoke();
            }
        }
        #endregion
        
        
        private static RuntimeCore _Instance;
        // ReSharper disable once InconsistentNaming
        public static RuntimeCore Instance => _Instance ??= new RuntimeCore();
        
        #region Private observables
        private readonly ObservableList<GameObject> m_SelectedObjects = new();
        private readonly Observable<Type> m_ActiveSpellType = new();
        public IReadOnlyCollection<GameObject> selectedObjects => m_SelectedObjects.items;
        #endregion
        
        #region Private properties
        private readonly ObservableList<CustomFunction> m_CustomFunctions = new();
        #endregion
        
        #region Public properties
        /// <summary>
        /// Contains all spells that are available in the project
        /// </summary>
        public SpellMetadata[] spellMetadata { get; private set; }

        #endregion
        
        #region Events
        public event Action OnSelectedObjectsChanged;
        #endregion
        
        #region Initialization

        private RuntimeCore()
        {
            /* Find all structures that implement the ISpell interface */
            var spellTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(ISpell).IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract)
                .Select(SpellAttribute.GetMetadata);
            var enumerable = spellTypes as SpellMetadata[] ?? spellTypes.ToArray();
            
            Console.Info("Metamatic Framework", $"Found {enumerable.Count()} spells");
            spellMetadata = enumerable.ToArray();
            
            
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            _Instance = new RuntimeCore();
        }
        #endregion
        
        #region Public methods
        public void SelectObject(GameObject gameObject, bool add = false)
        {
            if (!add)
            {
                m_SelectedObjects.Clear();
            }
            m_SelectedObjects.Add(gameObject);
            OnSelectedObjectsChanged?.Invoke();
        }
        
        public void DeselectObject(GameObject gameObject)
        {
            m_SelectedObjects.Remove(gameObject);
            OnSelectedObjectsChanged?.Invoke();
        }
        
        public void ToggleObjectSelection(GameObject gameObject, bool add = false)
        {
            if (m_SelectedObjects.Contains(gameObject))
            {
                m_SelectedObjects.Remove(gameObject);
                OnSelectedObjectsChanged?.Invoke();
            }
            else
            {
                if (!add)
                {
                    m_SelectedObjects.Clear();
                }
                m_SelectedObjects.Add(gameObject);
                OnSelectedObjectsChanged?.Invoke();
            }
        }
        
        public DisposableAction SwitchOnEverySelectedObject(Func<GameObject, CancellationSignal, UniTaskVoid> action)
        {
            return m_SelectedObjects.WatchEachLifecycle(action);
        }
        
        /// <summary>
        /// Subscribes to the event that is triggered when the selected objects change.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public DisposableAction WatchSelectObjectsChanged(Action action)
        {
            OnSelectedObjectsChanged += action;
            action();
            return new DisposableAction(() => OnSelectedObjectsChanged -= action);
        }
        
        public void SetActiveSpellType(Type spellType)
        {
            m_ActiveSpellType.value = spellType;
        }
        
        public DisposableAction WatchActiveSpellType(Action<Type> action)
        {
            return m_ActiveSpellType.Watch(action);
        }
        
        public DisposableAction OnSwitchActiveSpellType(Func<Type, CancellationSignal, UniTaskVoid> action)
        {
            return SequentialHandler<Type>.WatchAsync(m_ActiveSpellType.Watch, action);
        }

        public DisposableAction AddCustomFunction(string name, Action action)
        {
            var customFunction = new CustomFunction(name, action);
            m_CustomFunctions.Add(customFunction);
            return new DisposableAction(() => m_CustomFunctions.Remove(customFunction));
        }
        
        public DisposableAction WatchEachCustomFunctionLifecycle(Func<CustomFunction, CancellationSignal, UniTaskVoid> action)
        {
            return m_CustomFunctions.WatchEachLifecycle(action);
        }
        #endregion
    }
}