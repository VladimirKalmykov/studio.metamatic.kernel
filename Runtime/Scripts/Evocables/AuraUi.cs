using System;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using Metamatic;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace ClickTower2024
{
    public readonly struct Positioning
    {
        private readonly Func<(Vector3, Vector3)> m_GetPosition;
        
        public Positioning(Func<(Vector3, Vector3)> getPosition)
        {
            m_GetPosition = getPosition;
        }
        
        public (Vector3 position, Vector3 anchor) GetPlacement()
        {
            return m_GetPosition();
        }
    }

    public readonly struct AuraUi<T>: IAsyncDisposableAbortable where T: Enum
    {
        private static readonly ParamLessPool<AuraUiImpl<T>> Pool = new ParamLessPool<AuraUiImpl<T>>();
        public static AuraUi<T> Create(AuraUiImpl<T>.GetPositioning getPositioning)
        {
            var impl = Pool.Get();
            impl.Initialize(getPositioning);
            return new AuraUi<T>(impl);
        }
        private static void Return(AuraUiImpl<T> impl)
        {
            Pool.Return(impl);
        }
        
        private readonly AuraUiImpl<T> m_Impl;
        private readonly int m_Version;

        private AuraUiImpl<T> impl => m_Impl != null && m_Impl.version == m_Version ? m_Impl : null;

        public AuraUi(AuraUiImpl<T> impl)
        {
            m_Impl = impl;
            m_Version = impl.version;
        }
        
        public LiteMotel<T>.Reservation ReservePlace(T placeType, T alternativePlaceType, Func<T, Positioning, AsyncCancellation, UniTask> onReserve)
        {
            if (impl == null)
            {
                throw new InvalidOperationException("AuraUi is disposed");
            }
            return impl.ReservePlace(placeType, alternativePlaceType, onReserve);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DisposableAction SubscribeEmptyOnce(Action action)
        {
            return impl?.SubscribeEmptyOnce(action) ?? DisposableAction.Empty;
        }

        public async UniTask DisposeAsync()
        {
            try
            {
                await impl.DisposeAsync();
            }
            finally
            {
                Return(impl);
            }
        }

        public bool isAborted => impl?.isAborted ?? false;
        public void Abort()
        {
            impl?.Abort();
            Return(impl);
        }

        public DisposableAction AddAbortedListenerOnce(Action action)
        {
            return impl?.AddAbortedListenerOnce(action) ?? DisposableAction.Empty;
        }
    }
    public class AuraUiImpl<T>: IAsyncDisposableAbortable, IResettable where T: Enum
    {
        public delegate (Vector3 worldPosition, Vector3 anchor) GetPositioning(T placeType);
        private GetPositioning m_GetPositioning;
        private LiteMotel<T> m_Motel;
        public int version { get; private set; }
        
        public void Initialize(GetPositioning getPositioning)
        {
            m_GetPositioning = getPositioning;
            m_Motel = LiteMotel<T>.Create();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DisposableAction SubscribeEmptyOnce(Action action)
        {
            return m_Motel.SubscribeEmptyOnce(action);
        }

        public async UniTask DisposeAsync()
        {
            await m_Motel.DisposeAsync();
        }

        private event Action OnAborted;
        public bool isAborted { get; private set; }
        public void Abort()
        {
            isAborted = true;
            OnAborted?.Invoke();
        }
        
        /// <summary>
        /// Reserve a place in the aura ui.
        /// </summary>
        /// <param name="placeType"></param>
        /// <param name="alternativePlaceType"></param>
        /// <param name="onReserve"></param>
        /// <returns></returns>
        public LiteMotel<T>.Reservation ReservePlace(T placeType, T alternativePlaceType, Func<T, Positioning, AsyncCancellation, UniTask> onReserve)
        {
            return m_Motel.Book(placeType, alternativePlaceType, (bookedPlaceType, cancellation) =>
            {
                return onReserve(bookedPlaceType, new Positioning(() => m_GetPositioning(bookedPlaceType)), cancellation);
            });
        }

        public DisposableAction AddAbortedListenerOnce(Action action)
        {
            OnAborted += action;
            return new DisposableAction(() => OnAborted -= action);
        }

        public void Reset()
        {
            unchecked
            {
                version++;
            }
            m_GetPositioning = null;
            isAborted = false;
            OnAborted = null;
            m_Motel.Abort();
        }
    }
}