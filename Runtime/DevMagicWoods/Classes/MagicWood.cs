using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Magic
{
    public abstract class MagicWood: IDisposable, IExposedDisposed
    {
        public readonly string Name;
        public readonly string DisplayName;
        public LayerMask LayerMask;
        
        public MagicWood(string name, string displayName, LayerMask layerMask = default)
        {
            Name = name;
            DisplayName = displayName;
            LayerMask = layerMask;
        }

        public abstract UniTaskVoid OnActivate(CancellationSignal signal);

        public abstract void OnPress(Vector2 screenPosition);

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
    
    public abstract class AsyncMagicWood: MagicWood
    {
        private CancellationSignalSource m_DisposeSignalSource = CancellationSignalSource.Create();
        private bool m_IsReady;
        private event Action m_ReadyEvent;
        protected AsyncMagicWood(string name, string displayName, LayerMask layerMask = default) : base(name, displayName, layerMask)
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
            var tcs = SafeTaskCompletionSource.Create();
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