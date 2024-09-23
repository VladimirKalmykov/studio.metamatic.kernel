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
        private static RuntimeCore _Instance;
        // ReSharper disable once InconsistentNaming
        public static RuntimeCore Instance => _Instance ??= new RuntimeCore();
        
        #region Private observables
        #endregion
        
        #region Various fields
        
        
        private readonly ManagedList<GameObject> m_SelectedObjects = new();

        #endregion
        
        #region Public properties
        /// <summary>
        /// Contains all spells that are available in the project
        /// </summary>
        public SpellMetadata[] spellMetadata { get; private set; }

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
        }
        
        public void DeselectObject(GameObject gameObject)
        {
            m_SelectedObjects.Remove(gameObject);
        }
        
        public void ToggleObjectSelection(GameObject gameObject, bool add = false)
        {
            if (m_SelectedObjects.Contains(gameObject))
            {
                m_SelectedObjects.Remove(gameObject);
            }
            else
            {
                if (!add)
                {
                    m_SelectedObjects.Clear();
                }
                m_SelectedObjects.Add(gameObject);
            }
        }
        
        public DisposableAction ObserveSelectedObjects(Func<GameObject, CancellationSignal, UniTaskVoid> action)
        {
            return m_SelectedObjects.Observe(action);
        }
        #endregion
    }
}