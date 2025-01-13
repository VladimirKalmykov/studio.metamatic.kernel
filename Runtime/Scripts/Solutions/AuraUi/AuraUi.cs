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
        private readonly IVersioned m_VersionSource;
        private readonly int m_Version;
        private readonly Func<AnchoredPosition> m_GetAnchoredPosition;
        
        public Positioning(IVersioned versionSource, Func<AnchoredPosition> getAnchoredPosition)
        {
            m_VersionSource = versionSource;
            m_Version = versionSource.version;
            m_GetAnchoredPosition = getAnchoredPosition;
        }
        
        public AnchoredPosition GetAnchoredPosition()
        {
            if (m_VersionSource == null || m_VersionSource.version != m_Version)
            {
                return AnchoredPosition.zero;
            }
            return m_GetAnchoredPosition();
        }
    }

    /// <summary>
    /// Allows to create aura ui - a place where you can reserve a place for your ui elements around the specific target.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public readonly struct AuraUi<T>: IDisposableAbortable where T: Enum
    {
        private static readonly ParamLessPool<AuraUiImpl<T>> Pool = new ParamLessPool<AuraUiImpl<T>>();
        public static AuraUi<T> Create(System.Func<T,Metamatic.AnchoredPosition> getPositioning)
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
        
        public MotelReservation<T> ReservePlace(T placeType, T alternativePlaceType, Func<T, Positioning, AsyncCancellation, UniTask> onReserve)
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

        public void Dispose()
        {
            Abort();
        }
    }
    public class AuraUiImpl<T>: IDisposableAbortable, IResettable, IVersioned where T: Enum
    {
        private System.Func<T,Metamatic.AnchoredPosition> m_GetPositioning;
        private Motel<T> m_Motel;
        public int version { get; private set; }
        
        public void Initialize(System.Func<T,Metamatic.AnchoredPosition> getPositioning)
        {
            m_GetPositioning = getPositioning;
            m_Motel = Motel<T>.Create();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DisposableAction SubscribeEmptyOnce(Action action)
        {
            return m_Motel.SubscribeEmptyOnce(action);
        }

        private event Action OnAborted;
        public bool isAborted { get; private set; }
        public void Abort()
        {
            isAborted = true;
            OnAborted?.Invoke();
            Reset();
        }
        
        /// <summary>
        /// Reserve a place in the aura ui.
        /// </summary>
        /// <param name="placeType"></param>
        /// <param name="alternativePlaceType"></param>
        /// <param name="onReserve"></param>
        /// <returns></returns>
        public MotelReservation<T> ReservePlace(T placeType, T alternativePlaceType, Func<T, Positioning, AsyncCancellation, UniTask> onReserve)
        {
            return m_Motel.Book(placeType, alternativePlaceType, (bookedPlaceType, cancellation) =>
            {
                var positioning = new Positioning(this, () => m_GetPositioning(bookedPlaceType));
                return onReserve(
                    bookedPlaceType,
                    positioning, 
                    cancellation
                );
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

        public void Dispose()
        {
            Reset();
        }
    }
}