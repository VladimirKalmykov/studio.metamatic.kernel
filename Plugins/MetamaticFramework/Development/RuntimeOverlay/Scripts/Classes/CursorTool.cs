using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

namespace Metamatic.Framework
{
    public abstract class CursorTool: IDisposable, IExposedDisposed
    {
        #region Static
        private static List<(Type, MetamaticCursorToolAttribute)> _CachedCursorToolClasses;
        /// <summary>
        /// Return all CursorTool classes with MetamaticCursorToolAttribute
        /// </summary>
        /// <returns></returns>
        public static List<(Type type, MetamaticCursorToolAttribute metadata)> GetClasses()
        {
            if (_CachedCursorToolClasses != null) return _CachedCursorToolClasses;
            _CachedCursorToolClasses = new List<(Type, MetamaticCursorToolAttribute)>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();

                foreach (var type in types)
                {
                    // Проверяем, наследуется ли класс от Widget
                    if (type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(CursorTool)))
                    {
                        // Проверяем наличие атрибута MetamaticWidgetAttribute
                        var attribute = type.GetCustomAttribute<MetamaticCursorToolAttribute>();
                        if (attribute != null)
                        {
                            _CachedCursorToolClasses.Add((type, attribute));
                        }
                    }
                }
            }

            return _CachedCursorToolClasses;
        }
        #endregion
        
        public Vector2 ScreenPosition;
        public abstract UniTaskVoid OnActivate(DevToolsOverlay overlay, CancellationSignal signal);

        public abstract void OnPress(Vector2 screenPosition, InputModifiers modifiers);

        public abstract void OnRelease(Vector2 screenPosition);

        public abstract void OnMove(Vector2 screenPos);

        protected abstract void OnDispose();
        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }
            isDisposed = true;
            OnDisposed?.Invoke();
            OnDispose();
        }

        public bool isDisposed { get; private set; }
        public event Action OnDisposed;
    }
    
    public abstract class AsyncCursorTool: CursorTool
    {
        private CancellationSignalSource m_DisposeSignalSource = CancellationSignalSource.Create();
        private bool m_IsReady;
        private event Action m_ReadyEvent;
        protected AsyncCursorTool()
        {
            OnDisposed += () =>
            {
                m_DisposeSignalSource.TryCancel();
            };
        }

        protected void InitAsync()
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            OnInitAsync(Ready, m_DisposeSignalSource.Signal).Forget();
        }
        
        protected abstract UniTaskVoid OnInitAsync(Func<UniTask> ready, CancellationSignal signal);

        public UniTask WhenReady(CancellationSignal signal)
        {
            if (m_IsReady)
            {
                return UniTask.CompletedTask;
            }
            var tcs = SafeUniTaskCompletionSource.Create();
            DisposableAction registration = default;
            void OnReady()
            {
                // ReSharper disable once AccessToModifiedClosure
                registration.Dispose();
                m_ReadyEvent -= OnReady;
                tcs.TrySetCanceled();
            }
            m_ReadyEvent += OnReady;
            registration = signal.Register(() =>
            {
                // ReSharper disable once AccessToModifiedClosure
                registration.Dispose();
                m_ReadyEvent -= OnReady;
                tcs.TrySetCanceled();
            });
            return tcs.Task;
        }
        
        private UniTask Ready()
        {
            m_IsReady = true;
            m_ReadyEvent?.Invoke();
            return m_DisposeSignalSource.Signal.ToUniTask();
        }
    }
}