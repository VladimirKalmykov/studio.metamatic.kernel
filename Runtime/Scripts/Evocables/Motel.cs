using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace Metamatic
{
    public readonly struct MotelReservation<T>: IAsyncDisposableAbortable where T : Enum
    {
        private readonly MotelReservationInternal<T> m_Source;
        private readonly int m_Version;
        public T bookedRoom => (T)Enum.ToObject(typeof(T), m_Source?.bookedRoom ?? 0);

        public MotelReservation(MotelReservationInternal<T> source)
        {
            m_Source = source;
            m_Version = source.version;
        }
        
        private MotelReservationInternal<T> source => m_Source != null && m_Source.version == m_Version ? m_Source : null;
        public T preferredRooms => (T)Enum.ToObject(typeof(T), source?.preferredRooms ?? 0);
        public T alternativeRooms => (T)Enum.ToObject(typeof(T), source?.alternativeRooms ?? 0);

        public void SetPreferredRooms(T preferredRooms) => source?.SetPreferredRooms(preferredRooms);
        public void SetAlternativeRooms(T alternativeRooms) => source?.SetAlternativeRooms(alternativeRooms);
        public void SetPreferredAndAlternativeRooms(T preferredRooms, T alternativeRooms) => source?.SetPreferredAndAlternativeRooms(preferredRooms, alternativeRooms);
        
        public UniTask DisposeAsync()
        {
            return source?.ReturnAsync() ?? UniTask.CompletedTask;
        }

        public bool isAborted => source?.isAborted ?? true;
        public void Abort()
        {
            source?.Abort();
        }

        public DisposableAction AddBookedOutListener(Action action)
        {
            var thisSource = source;
            if (thisSource == null)
            {
                action();
                return DisposableAction.Empty;
            }
            thisSource.OnBookedOut += action;
            
            return DisposableAction.Create(() => thisSource.OnBookedOut -= action);
        }

        public UniTask WhenBookedOutMoment(Func<bool> predicate, Signal abortSignal)
        {
            var thisSource = source;
            if (thisSource == null)
            {
                return UniTask.CompletedTask;
            }
            
            var cts = SafeUniTaskCompletionSource.Create();
            
            void Handler()
            {
                if (predicate())
                {
                    cts.TrySetResult();
                }
            }
            
            thisSource.OnBookedOut += Handler;
            var registration = abortSignal.Register(() => cts.TrySetCanceled());
            
            return cts.Task
                .ContinueWith(() =>
                {
                    registration.Dispose();
                    thisSource.OnBookedOut -= Handler;
                });
        }

        public DisposableAction AddAbortedListenerOnce(Action action)
        {
            var thisSource = source;
            if (thisSource == null)
            {
                action();
                return DisposableAction.Empty;
            }
            return source.AddAbortedListenerOnce(action);
        }

        public bool isDisposedAsync => source == null || source.isReturning;
    }

    public class MotelReservationInternal<T>: IDisposable, IAsyncReturnable, IResettable where T: Enum
    {
        #region Static
        private static ParamLessPool<MotelReservationInternal<T>> m_Pool = new ParamLessPool<MotelReservationInternal<T>>();
        public static MotelReservationInternal<T> GetFromPool()
        {
            return m_Pool.Get();
        }
        public static void ReturnToPool(MotelReservationInternal<T> reservation)
        {
            m_Pool.Release(reservation);
        }
        #endregion
        
        private MotelInternal<T> m_Motel;
        public long preferredRooms { get; private set; }
        public long alternativeRooms { get; private set; }
        private Func<T, AsyncCancellation, UniTask> m_OnRoomBookedHandler;
        /// <summary>
        /// The reservation is satisfied if current booked room is equal to one of the preferred room.
        /// </summary>
        private bool m_Satisfied;
        /// <summary>
        /// The reservation is alt-satisfied if current booked room is equal to one of the alternative room.
        /// The reservation is alt-satisfied too if the reservation is satisfied.
        /// </summary>
        private bool m_AltSatisfied;
        
        /// <summary>
        /// Container current booked room.
        /// </summary>
        [UsedImplicitly]
        public long bookedRoom { get; private set; }
        
        /// <summary>
        /// This cancellation source is used to cancel the booking or rebooking process.
        /// It updates each time the guest is booking in to the next room.
        /// </summary>
        private AsyncCancellationSource m_BookingCancellationSource;
        private bool m_IsDisposed;
        
        /// <summary>
        /// Rebooking task contains the task provided by Rebook method.
        /// </summary>
        private UniTask m_RebookingTask = UniTask.CompletedTask;
        private DisposableAction m_BookedRoomsWatcher;

        /// <summary>
        /// Contains motel booking rooms committed by this reservation. It allows to avoid double booking.
        /// </summary>
        private int m_HereCommitedBookedRoomsVersion = -1;
        
        public int version { get; private set; }
        
        /// <summary>
        /// This event is called when the guest is booked out from the room.
        /// </summary>
        public event Action OnBookedOut;
        
        public void Initialize(MotelInternal<T> motel, T preferredRooms, T alternativeRooms,
            Func<T, AsyncCancellation, UniTask> onRoomBooked)
        {
            m_Motel = motel;
            this.preferredRooms = Convert.ToInt64(preferredRooms);
            this.alternativeRooms = Convert.ToInt64(alternativeRooms);
            m_OnRoomBookedHandler = onRoomBooked;
            m_Motel.InternalRegisterReservation(this);
            m_BookingCancellationSource = AsyncCancellationSource.Create();
            InternalTryToBookIn();
        }
        
        public void SetPreferredRooms(T preferredRooms)
        {
            var nextPreferredRooms = Convert.ToInt64(preferredRooms);
            if (this.preferredRooms == nextPreferredRooms)
            {
                return;
            }
            this.preferredRooms = nextPreferredRooms;
            InternalTryToBookIn();
        }
        
        public void SetAlternativeRooms(T alternativeRooms)
        {
            var nextAlternativeRooms = Convert.ToInt64(alternativeRooms);
            if (this.alternativeRooms == nextAlternativeRooms)
            {
                return;
            }
            this.alternativeRooms = nextAlternativeRooms;
            InternalTryToBookIn();
        }
        
        public void SetPreferredAndAlternativeRooms(T preferredRooms, T alternativeRooms)
        {
            var nextPreferredRooms = Convert.ToInt64(preferredRooms);
            var nextAlternativeRooms = Convert.ToInt64(alternativeRooms);
            
            if (this.preferredRooms == nextPreferredRooms && this.alternativeRooms == nextAlternativeRooms)
            {
                return;
            }
            
            this.preferredRooms = nextPreferredRooms;
            this.alternativeRooms = nextAlternativeRooms;
            InternalTryToBookIn();
        }
        
        /// <summary>
        /// Find the next room to book in.
        /// </summary>
        /// <returns></returns>
        private (long roomToBookIn, bool satisfied, bool altSatisfied) FindRoomToBookIn()
        {
            var currentBookedRooms = m_Motel.bookedRooms & ~bookedRoom;
            var freeRooms = preferredRooms & ~currentBookedRooms;
            var nextBookedRoom = 0L;
            var satisfied = true;
            var altSatisfied = true;
            if (freeRooms == 0)
            {
                satisfied = false;
                freeRooms = alternativeRooms & ~currentBookedRooms;
            }
            
            if (freeRooms == 0)
            {
                altSatisfied = false;
            }
            else
            {
                nextBookedRoom = freeRooms & (~freeRooms + 1);
            }
            return (nextBookedRoom, satisfied, altSatisfied);
        }

        private void InternalTryToBookIn()
        {
            if (m_IsDisposed)
            {
                return;
            }
            var (roomToBookIn, satisfied, altSatisfied) = FindRoomToBookIn();
            m_Satisfied = satisfied;
            m_AltSatisfied = altSatisfied;
            
            if (!m_Satisfied || !m_AltSatisfied)
            {
                if (m_BookedRoomsWatcher.isEmpty)
                {
                    m_BookedRoomsWatcher = m_Motel.SubscribeToBookedRoomsChange(InternalTryToBookIn);
                }
            }
            else
            {
                m_BookedRoomsWatcher.Dispose();
                m_BookedRoomsWatcher = default;
            }
            
            if (roomToBookIn != bookedRoom)
            {
                BookInToNextRoom(roomToBookIn);
            }
        }

        private bool m_BookingIsInProgress;
        private long m_NextRoomInQueue = -1;

        private void BookInToNextRoom(long nextRoom)
        {
            while (true)
            {
                // If the booking is in progress, we should defer rebook until receive a new rebooking task.
                if (m_BookingIsInProgress)
                {
                    m_NextRoomInQueue = nextRoom;
                    return;
                }

                m_BookingIsInProgress = true;
                m_RebookingTask = PerformRebooking(m_RebookingTask, nextRoom);
                m_BookingIsInProgress = false;
                m_Motel.CommitBookedRooms();
                if (m_NextRoomInQueue > -1)
                {
                    var nextRoomInQueue = m_NextRoomInQueue;
                    m_NextRoomInQueue = -1;
                    nextRoom = nextRoomInQueue;
                    continue;
                }

                break;
            }
        }

        /// <summary>
        /// This method is called when the guest is trying to book in (or rebook) to specific room.
        /// </summary>
        /// <param name="previousRebookingTask"></param>
        /// <param name="nextRoom"></param>
        private async UniTask PerformRebooking(UniTask previousRebookingTask, long nextRoom)
        {
            bookedRoom = nextRoom;
            m_Motel.bookedRooms |= nextRoom;
            using var previousBookingAsyncCancellation = m_BookingCancellationSource.TryCancelAsync();
            m_BookingCancellationSource = AsyncCancellationSource.Create();
            var nextCancellation = m_BookingCancellationSource.Cancellation;
            try
            {
                using var currentBookingAbortListener = m_BookingCancellationSource.AddAbortedListenerOnce(previousBookingAsyncCancellation);
                await previousRebookingTask;
            }
            catch (OperationCanceledException)
            {
                // Skip
            }
            catch (Exception e)
            {
                Console.Error("Motel", e);
            }
            finally
            {
                try
                {
                    if (nextRoom != 0)
                    {
                        
                        if (Application.isPlaying)
                        {
                            await UniTask.NextFrame();
                            nextCancellation.ThrowIfCancellationRequested();
                        }
                        await m_OnRoomBookedHandler((T)Enum.ToObject(typeof(T), nextRoom), nextCancellation);
                    }
                }
                finally
                {
                    m_Motel.bookedRooms &= ~nextRoom;
                    bookedRoom = 0;
                    OnBookedOut?.Invoke();
                    m_Motel.CommitBookedRooms();
                }
            }
        }

        [UsedImplicitly]
        public bool isReturning { get; private set; }
        public async UniTask ReturnAsync()
        {
            if (isReturning)
            {
                return;
            }
            isReturning = true;
            m_BookedRoomsWatcher.Dispose();
            m_BookedRoomsWatcher = default;
            try
            {
                using var asyncCancellation = m_BookingCancellationSource.TryCancelAsync();
                await m_RebookingTask;
            }
            finally
            {
                isReturning = false;
                Abort();
            }
        }

        private event Action OnAborted;
        public bool isAborted { get; private set; }
        public void Abort()
        {
            if (isAborted || m_IsDisposed)
            {
                return;
            }
            isAborted = true;
            m_BookedRoomsWatcher.Dispose();
            m_BookingCancellationSource.Abort();
            if (bookedRoom != 0)
            {
                OnBookedOut?.Invoke();
            }
            OnAborted?.Invoke();
            OnAborted = null;
            ReturnToPool(this);
        }

        public DisposableAction AddAbortedListenerOnce(Action action)
        {
            OnAborted += action;
            return DisposableAction.Create(() => OnAborted -= action);
        }

        public void Reset()
        {
            unchecked
            {
                version++;
            }

            isAborted = false;
            m_Motel.InternalUnregisterReservation(this);
            m_Motel = null;
        }

        public void Dispose()
        {
            Abort();
        }
    }

    public readonly struct Motel<T> : IDisposableAbortable where T : Enum
    {
        public static Motel<T> Create()
        {
            var motel = MotelInternal<T>.GetFromPool();
            return new Motel<T>(motel);
        }
        private readonly MotelInternal<T> m_Motel;
        private readonly int m_Version;
        private MotelInternal<T> motel => m_Motel != null && m_Motel.version == m_Version ? m_Motel : null;
        
        public T bookedRooms => (T)Enum.ToObject(typeof(T), motel?.bookedRooms ?? 0);

        public Motel(MotelInternal<T> motel)
        {
            m_Motel = motel;
            m_Version = motel.version;
        }
        
        public MotelReservation<T> Book(T preferredRooms, T alternativeRooms,
            Func<T, AsyncCancellation, UniTask> onRoomBooked)
        {
            return motel?.Book(preferredRooms, alternativeRooms, onRoomBooked) ?? new MotelReservation<T>(null);
        }

        public DisposableAction SubscribeEmptyOnce(Action action)
        {
            var thisMotel = motel;
            if (thisMotel == null)
            {
                action();
                return DisposableAction.Empty;
            }
            return motel.SubscribeToEmptyOnce(action);
        }

        private void Return()
        {
            MotelInternal<T>.ReturnToPool(motel);
        }

        public void Dispose()
        {
            var thisMotel = motel;
            if (thisMotel != null)
            {
                Return();
            }
        }

        public bool isAborted => motel != null;
        public void Abort()
        {
            Dispose();
        }

        public DisposableAction AddAbortedListenerOnce(Action action)
        {
            var thisMotel = motel;
            if (thisMotel == null)
            {
                action();
                return DisposableAction.Empty;
            }
            
            thisMotel.OnReset += Handler;
            
            return DisposableAction.Create(() => thisMotel.OnReset -= Handler);

            void Handler()
            {
                thisMotel.OnReset -= Handler;
                action();
            }
        }
    }
    public class MotelInternal<T>: IDisposable, IResettable where T: Enum
    {
        #region Static
        private static readonly ParamLessPool<MotelInternal<T>> Pool = new ParamLessPool<MotelInternal<T>>();
        public static MotelInternal<T> GetFromPool()
        {
            return Pool.Get();
        }
        public static void ReturnToPool(MotelInternal<T> motel)
        {
            Pool.Return(motel);
        }
        #endregion
        
        public long bookedRooms { get; internal set; }
        private event Action OnBookedRoomsChanged;
        private Observable<int> m_TotalGuests = new Observable<int>();
        private List<MotelReservationInternal<T>> m_Reservations = new List<MotelReservationInternal<T>>();
        public int version { get; private set; }
        private bool m_IsDisposed;
        public ObservableReadOnly<int> totalGuests => m_TotalGuests;
        public event Action OnReset;
        public event Action OnEmpty;

        public MotelReservation<T> Book(T preferredRooms, T alternativeRooms,
            Func<T, AsyncCancellation, UniTask> onRoomBooked)
        {
            var reservation = MotelReservationInternal<T>.GetFromPool();
            reservation.Initialize(this, preferredRooms, alternativeRooms, onRoomBooked);
            return new MotelReservation<T>(reservation);
        }
        
        internal void InternalRegisterReservation(MotelReservationInternal<T> reservation)
        {
            m_Reservations.Add(reservation);
        }
        
        internal void InternalUnregisterReservation(MotelReservationInternal<T> reservation)
        {
            m_Reservations.Remove(reservation);
            if (m_Reservations.Count == 0)
            {
                OnEmpty?.Invoke();
            }
        }
        
        public DisposableAction SubscribeToEmptyOnce(Action action)
        {
            OnEmpty += action;
            return DisposableAction.Create(() => OnEmpty -= action);
        }
        
        public void CommitBookedRooms()
        {
            OnBookedRoomsChanged?.Invoke();
        }
        
        public DisposableAction SubscribeToBookedRoomsChange(Action action)
        {
            OnBookedRoomsChanged += action;
            return DisposableAction.Create(() => OnBookedRoomsChanged -= action);
        }

        public void Reset()
        {
            unchecked
            {
                version++;
            }
            bookedRooms = 0;
            OnReset?.Invoke();
            OnEmpty = null;
            /* Abort all reservations */
            foreach (var reservation in m_Reservations)
            {
                reservation.Abort();
            }
            m_Reservations.Clear();
        }

        public void Dispose()
        {
            if (m_IsDisposed)
            {
                return;
            }
            m_IsDisposed = true;
            Reset();
        }
    }
}