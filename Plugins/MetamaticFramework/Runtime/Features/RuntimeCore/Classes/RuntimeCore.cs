using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

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
        public IReadOnlyCollection<GameObject> selectedObjects => m_SelectedObjects.items;
        #endregion
        
        #region Private properties
        private readonly ObservableList<CustomFunction> m_CustomFunctions = new();
        #endregion
        
        #region Events
        public event Action OnSelectedObjectsChanged;
        #endregion
        
        #region Initialization

        private RuntimeCore()
        {
            // Subscribe to selection
            #if UNITY_EDITOR
            Selection.selectionChanged += () =>
            {
                m_SelectedObjects.Synchronize(Selection.gameObjects);
                OnSelectedObjectsChanged?.Invoke();
            };
            // Check currently selected object
            m_SelectedObjects.Synchronize(Selection.gameObjects);
            #endif
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            _Instance = new RuntimeCore();
        }
        #endregion
        
        private bool TakeFirst(GameObject obj, int index)
        {
            return index == 0;
        }
        
        #region Public methods
        public void SelectObject(GameObject gameObject, bool add = false)
        {
            if (!add)
            {
                m_SelectedObjects.Clear();
            }
            m_SelectedObjects.Add(gameObject);
            OnSelectedObjectsChanged?.Invoke();
            #if UNITY_EDITOR
            Selection.activeObject = gameObject;
            #endif
        }
        
        public void DeselectObject(GameObject gameObject)
        {
            if (!m_SelectedObjects.Contains(gameObject)) return;
            m_SelectedObjects.Remove(gameObject);
            OnSelectedObjectsChanged?.Invoke();
#if UNITY_EDITOR
            Selection.activeObject = null;
#endif
        }
        
        public void ClearSelection()
        {
            m_SelectedObjects.Clear();
            OnSelectedObjectsChanged?.Invoke();
#if UNITY_EDITOR
            Selection.activeObject = null;
#endif
        }
        
        public void ToggleObjectSelection(GameObject gameObject, bool add = false)
        {
            if (m_SelectedObjects.Contains(gameObject))
            {
                DeselectObject(gameObject);
            }
            else
            {
                SelectObject(gameObject, add);
            }
        }

        public DisposableAction WatchFirstSelectObjectLifecycle(Func<GameObject, CancellationSignal, UniTaskVoid> action)
        {
            return AsyncDisposableOperation.Call(async (signal) =>
            {
                using var selectedObject = new Observable<GameObject>();
                using var watchSelectedObject = selectedObject.WatchAsync(action);
                using var watchSelectedObjects = m_SelectedObjects.WatchEach((obj, index) =>
                {
                    selectedObject.SetUniqValue(obj);
                }, TakeFirst);
                await signal;
            });
        }
        
        public DisposableAction WatchEverySelectedObjectLifecycle(Func<GameObject, int, CancellationSignal, UniTaskVoid> action, Func<GameObject, int, bool> filter = null)
        {
            return m_SelectedObjects.WatchEachLifecycle(action, filter);
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

        public DisposableAction AddCustomFunction(string name, Action action)
        {
            var customFunction = new CustomFunction(name, action);
            m_CustomFunctions.Add(customFunction);
            return new DisposableAction(() => m_CustomFunctions.Remove(customFunction));
        }
        
        public DisposableAction WatchEachCustomFunctionLifecycle(Func<CustomFunction, int, CancellationSignal, UniTaskVoid> action)
        {
            return m_CustomFunctions.WatchEachLifecycle(action);
        }
        #endregion
    }
}