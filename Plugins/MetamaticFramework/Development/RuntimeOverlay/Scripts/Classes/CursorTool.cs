using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

namespace Metamatic.Framework
{
    public abstract class CursorTool: IDisposable, IExposedDisposed
    {
        public readonly string Name;
        public readonly string DisplayName;
        public readonly string IconPath;
        public Vector2 ScreenPosition;
        public LayerMask LayerMask;
        
        public CursorTool(string name, string displayName, string icon, LayerMask layerMask = default)
        {
            Name = name;
            DisplayName = displayName;
            LayerMask = layerMask;
            IconPath = icon;
        }

        public abstract UniTaskVoid OnActivate(Overlay overlay, CancellationSignal signal);

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
        protected AsyncCursorTool(string name, string displayName, string icon, LayerMask layerMask = default) : base(name, displayName, icon, layerMask)
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