
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public readonly struct LiteMotel<T> : IAsyncDisposableAbortable where T: Enum
    {
        public readonly struct Reservation: IAsyncDisposableAbortable
        {
            private readonly AbortableAsyncOperation m_Operation;
            private readonly Observable<(long preferredRooms, long alternativeRooms)> m_Config;
            
            public Reservation(AbortableAsyncOperation operation, Observable<(long preferredRooms, long alternativeRooms)> config)
            {
                m_Operation = operation;
                m_Config = config;
            }
            
            public void SetPreferredRooms(T preferredRooms)
            {
                m_Config.Value = (Convert.ToInt64(preferredRooms), m_Config.Value.alternativeRooms);
            }
            
            public void SetAlternativeRooms(T alternativeRooms)
            {
                m_Config.Value = (m_Config.Value.preferredRooms, Convert.ToInt64(alternativeRooms));
            }
            
            public void SetRooms(T preferredRooms, T alternativeRooms)
            {
                m_Config.Value = (Convert.ToInt64(preferredRooms), Convert.ToInt64(alternativeRooms));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public UniTask DisposeAsync()
            {
                return m_Operation.DisposeAsync();
            }

            public bool isAborted => m_Operation.isAborted;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Abort()
            {
                m_Operation.Abort();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public DisposableAction AddAbortedListenerOnce(Action action)
            {
                if (m_Operation.isAborted)
                {
                    action.Invoke();
                    return DisposableAction.Empty;
                }
                return m_Operation.AddAbortedListenerOnce(action);
            }
        }
        private static readonly ParamLessPool<LiteMotelInternal<T>> Pool = new ParamLessPool<LiteMotelInternal<T>>();
        public static LiteMotel<T> Create()
        {
            var impl = Pool.Get();
            return new LiteMotel<T>(impl);
        }
        private static void Return(LiteMotelInternal<T> impl)
        {
            Pool.Return(impl);
        }
        private readonly LiteMotelInternal<T> m_Impl;
        private readonly int m_Version;

        public T bookedRooms
        {
            get
            {
                var impl = Impl;
                return impl != null ? impl.bookedRooms : default;
            }
        }
        public LiteMotel(LiteMotelInternal<T> impl)
        {
            m_Impl = impl;
            m_Version = impl.version;
        }
        
        private LiteMotelInternal<T> Impl => m_Impl != null && m_Impl.version == m_Version ? m_Impl : null;
        
        public DisposableAction SubscribeEmptyOnce(Action action)
        {
            var impl = Impl;
            if (impl != null)
            {
                return impl.WatchEmptyOnce(action);
            }
            return DisposableAction.Empty;
        }
        public async UniTask DisposeAsync()
        {
            var impl = Impl;
            if (impl != null)
            {
                await impl.DisposeAsync();
                Return(m_Impl);
            }
        }
        
        public Reservation Book(T preferredRooms, T alternativeRooms, Func<T, AsyncCancellation, UniTask> onRoomBooked)
        {
            var impl = Impl;
            if (impl == null)
            {
                throw new InvalidOperationException("Motel is disposed.");
            }
            return impl.Book(preferredRooms, alternativeRooms, onRoomBooked);
        }

        public bool isAborted => Impl?.isAborted ?? true;
        public void Abort()
        {
            var impl = Impl;
            if (impl != null)
            {
                impl.Abort();
                Return(m_Impl);
            }
        }

        public DisposableAction AddAbortedListenerOnce(Action action)
        {
            var impl = Impl;
            if (impl != null)
            {
                return impl.AddAbortedListenerOnce(action);
            }
            return default;
        }
    }
    public class LiteMotelInternal<T>: IAsyncDisposableAbortable, IResettable where T: Enum
    {
        private Observable<long> m_BookedRooms = new Observable<long>();
        private Observable<int> m_TotalGuests = new Observable<int>();
        private event Action OnAborted;
        private event Action OnEmpty;
        private HashSet<IAsyncDisposableAbortable> m_GuestOperations = new HashSet<IAsyncDisposableAbortable>();
        public int version { get; private set; }
        
        public T bookedRooms => (T)Enum.ToObject(typeof(T), m_BookedRooms.value);
        public LiteMotelInternal()
        {
            var enumType = typeof(T);
            if (!Attribute.IsDefined(enumType, typeof(FlagsAttribute)))
            {
                throw new ArgumentException($"Enum {enumType.Name} must have argument [Flags].");
            }
        }
        
        /// <summary>
        /// Watch the motel for empty rooms. The callback will be called once when the motel is empty.
        /// </summary>
        /// <param name="onEmpty"></param>
        /// <returns></returns>
        public DisposableAction WatchEmptyOnce(Action onEmpty)
        {
            OnEmpty += OnEmptyOnce;
            return new DisposableAction(() =>
            {
                OnEmpty -= OnEmptyOnce;
            });

            void OnEmptyOnce()
            {
                onEmpty();
                OnEmpty -= OnEmptyOnce;
            }
        }

        public LiteMotel<T>.Reservation Book(T preferredRooms, T alternativeRooms, Func<T, AsyncCancellation, UniTask> onRoomBooked)
        {
            Observable<(long preferredRooms, long alternativeRooms)> observableConfig = ParamLessPool<Observable<(long prefferedRooms, long alternativeRooms)>>.GetShared();
            var operation = AbortableAsyncOperation.Call(async (operationCancellation) =>
            {
                m_TotalGuests.Value++;
                var preferredRoomsValue = Convert.ToInt64(preferredRooms);
                var alternativeRoomsValue = Convert.ToInt64(alternativeRooms);
                observableConfig.Value = (preferredRoomsValue, alternativeRoomsValue);
                var bookingCancellationSource = AsyncCancellationSource.Create();
                
                long bookedRoom = 0;
                UniTask currentBookingTask = UniTask.CompletedTask;
                UniTask bookOutInTransitionTask = UniTask.CompletedTask;
                try
                {
                    var bookingInSubscription = BookInOrSubscribe(m_BookedRooms.Value, observableConfig,
                        nextRoom =>
                        {

                            bookingCancellationSource.TryCancelAsync();
                            bookingCancellationSource = AsyncCancellationSource.Create();
                            bookOutInTransitionTask.Forget();
                            bookOutInTransitionTask = OnBookOutInTransition(currentBookingTask, bookedRoom, nextRoom);
                            bookedRoom = nextRoom;
                            currentBookingTask = nextRoom != 0
                                ? onRoomBooked.Invoke((T)Enum.ToObject(typeof(T), nextRoom),
                                    bookingCancellationSource.Cancellation)
                                : UniTask.CompletedTask;
                        });
                    await operationCancellation.Signal;
                    bookingInSubscription.Dispose();
                    if (operationCancellation.isCancelledAsync)
                    {
                        bookingCancellationSource.TryCancelAsync();
                        operationCancellation.AbortSignal.Register(bookingCancellationSource.TryCancelSync);
                        await bookOutInTransitionTask;
                        
                        if (bookedRoom != 0)
                        {
                            await OnBookOutInTransition(currentBookingTask, bookedRoom, 0);
                        }
                        else
                        {
                            currentBookingTask.Forget();
                        }
                    }
                    else
                    {
                        bookingCancellationSource.TryCancelSync();
                        /* Forget currently processed booking out/in task */
                        bookOutInTransitionTask.Forget();
                        if (bookedRoom != 0)
                        {
                            OnBookOutInTransition(currentBookingTask, bookedRoom, 0).Forget();
                        }
                        else
                        {
                            currentBookingTask.Forget();
                        }
                    }
                }
                finally
                {
                    m_TotalGuests.Value--;
                }
            });
            m_GuestOperations.Add(operation);
            operation.AddDisposeListenerOnce((bool disposedAsync) =>
            {
                ParamLessPool<Observable<(long, long)>>.ReturnShared(observableConfig);
                m_GuestOperations.Remove(operation);
                if (m_GuestOperations.Count == 0)
                {
                    OnEmpty?.Invoke();
                }
            });
            return new LiteMotel<T>.Reservation(operation, observableConfig);
        }

        private async UniTask OnBookOutInTransition(UniTask currentBookingTask, long prevRoom, long nextRoom)
        {
            try
            {
                if (nextRoom != 0)
                {
                    m_BookedRooms.Value = m_BookedRooms.value | nextRoom;
                }
                await currentBookingTask;
            }
            finally
            {
                if (prevRoom != 0)
                {
                    m_BookedRooms.Value = m_BookedRooms.value & ~prevRoom;
                }
            }
        }

        private DisposableAction BookInOrSubscribe(long bookedRooms, Observable<(long preferredRooms, long alternativeRooms)> config, Action<long> onRoomBooked)
        {
            bool satisfied = false;
            bool altSatisfied = false;
            long bookedRoom = 0;

            return config.WatchAsync(async (configData, signal) =>
            {
                var result = TryBookIn(bookedRooms & ~bookedRoom, configData.preferredRooms, configData.alternativeRooms);
                
                satisfied = result.satisfied;
                altSatisfied = result.altSatisfied;
                if (result.bookedRoom != bookedRoom)
                {
                    bookedRoom = result.bookedRoom;
                    onRoomBooked.Invoke(bookedRoom);
                }

                DisposableAction subscription = default;
                if (!satisfied || !altSatisfied)
                {
                    // Subscribe to free rooms
                    subscription = m_BookedRooms.Watch(nextBookedRooms =>
                    {
                        /*
                         * Use directly m_BookedRooms.Value instead of nextBookedRooms, because the value of nextBookedRooms can be changed by other subscribers.
                         */
                        var checkIn = TryBookIn(m_BookedRooms.Value, configData.preferredRooms, altSatisfied ? 0 : configData.alternativeRooms);
                        if (checkIn.bookedRoom != 0)
                        {
                            satisfied = checkIn.satisfied;
                            altSatisfied = checkIn.altSatisfied;
                            if (satisfied)
                            {
                                subscription.Dispose();
                                subscription = default;
                            }
                            onRoomBooked.Invoke(checkIn.bookedRoom);
                        }
                    }, true);
                }

                await signal;
                subscription.Dispose();
                subscription = default;
            });
        }
        
        private (long bookedRoom, bool satisfied, bool altSatisfied) TryBookIn(long bookedRooms, long preferredRooms, long alternativeRooms)
        {
            var freeRooms = preferredRooms & ~bookedRooms;
            var bookedRoom = 0L;
            var satisfied = true;
            var altSatisfied = true;
            if (freeRooms == 0)
            {
                satisfied = false;
                freeRooms = alternativeRooms & ~bookedRooms;
            }
            
            if (freeRooms == 0)
            {
                altSatisfied = false;
            }
            else
            {
                bookedRoom = freeRooms & (~freeRooms + 1);
            }
            return (bookedRoom, satisfied, altSatisfied);
        }

        public UniTask DisposeAsync()
        {
            /* Abort all guest operations */
            foreach (var operation in m_GuestOperations.ToArray())
            {
                operation.Abort();
            }
            var tsc = SafeUniTaskCompletionSource.Create();
            DisposableAction subscription = default;
            subscription = m_TotalGuests.Watch(totalGuests =>
            {
                if (totalGuests == 0)
                {
                    subscription.Dispose();
                    tsc.TrySetResult();
                }
            }, false);
            return tsc.Task;
        }

        public bool isAborted { get; private set; }

        public void Abort()
        {
            isAborted = true;
            OnAborted?.Invoke();
            OnAborted = null;
            /* Abort all guest operations */
            foreach (var operation in m_GuestOperations)
            {
                operation.Abort();
            }
        }

        public DisposableAction AddAbortedListenerOnce(Action action)
        {
            OnAborted += action;
            return new DisposableAction(() =>
            {
                OnAborted -= action;
            });
        }

        public void Reset()
        {
            unchecked
            {
                version++;
            }
            /* Abort all guest operations */
            foreach (var operation in m_GuestOperations)
            {
                operation.Abort();
            }
            m_BookedRooms.Value = 0;
            m_TotalGuests.Value = 0;
        }
    }
}