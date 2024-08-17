using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public class Motel<T> : IUniTaskAsyncDisposable, IExposedDisposed where T : Enum
    {
        private readonly Dictionary<T, MotelReservation<T>> m_Rooms;
        private Queue<MotelReservation<T>> m_WaitingQueue = new();
        private Queue<MotelReservation<T>> m_ContinueWaitingQueue = new();
        internal CancellationTokenSource m_AbortTokenSource = new();
        
        private IEnumerable<T> m_AllRooms;
        private bool m_IsDisposeProcessing;
        private UniTask? m_ReleaseAllReservationsTask;
        private int m_ReservationsCount;
        
        public CancellationToken abortToken => m_AbortTokenSource.Token;

        /// <summary>
        /// On motel has no reservations.
        /// </summary>
        public event Action OnEmpty;
        
        public Motel(object[] dependencies = null)
        {
            m_AllRooms = Enum.GetValues(typeof(T)).Cast<T>().Where(x => !x.Equals(default(T)));
            m_Rooms = new Dictionary<T, MotelReservation<T>>();
            if (dependencies != null)
            {
                OnDisposed += new ObserveExposedDisposable(dependencies, Abort);
            }
        }

        public MotelReservation<T> Reserve(T preferredRooms, Func<T, MotelReservation<T>, UniTask> onSettle,
            bool strict = false, object[] dependencies = null)
        {
            if (isDisposed || m_AbortTokenSource.IsCancellationRequested)
            {
                throw new ObjectDisposedException(nameof(Motel<T>));
            }
            var reservation = new MotelReservation<T>(this, preferredRooms, onSettle, strict, dependencies);
            var preferredRoomsArray = Bitmask.ExtractArray(preferredRooms);
            var settled = false;
            foreach (var room in preferredRoomsArray)
            {
                if (!m_Rooms.ContainsKey(room) || m_Rooms[room] == null)
                {
                    m_Rooms[room] = reservation;
                    m_ReservationsCount++;
                    reservation.CheckIn(room).Forget();
                    settled = true;
                    break;
                }
            }

            if (!settled)
            {
                if (!strict)
                {
                    /* Find first available room */
                    foreach (var room in m_AllRooms)
                    {
                        if (!m_Rooms.ContainsKey(room) || m_Rooms[room] == null)
                        {
                            m_Rooms[room] = reservation;
                            m_ReservationsCount++;
                            reservation.CheckIn(room).Forget();
                            settled = true;
                            break;
                        }
                    }
                }
            }
            
            if (!settled)
            {
                /* Add to waiting queue */
                m_WaitingQueue.Enqueue(reservation);
            }

            return reservation;
        }
        
        /// <summary>
        /// Check whether the room is free.
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        public bool IsFree(T room)
        {
            if (m_Rooms.TryGetValue(room, out var reservation))
            {
                return reservation == null;
            }

            return true;
        }
        
        public void AddToWaitingList(MotelReservation<T> reservation)
        {
            m_WaitingQueue.Enqueue(reservation);
        }
        
        internal void ReleaseReservation(MotelReservation<T> reservation)
        {
            var freeRoom = reservation.room;
            if (m_Rooms.TryGetValue(freeRoom, out var current) && current == reservation)
            {
                m_Rooms[freeRoom] = null;
                m_ReservationsCount--;
            }
            if (m_IsDisposeProcessing)
            {
                /* Bail out if disposing */
                return;
            }
            MotelReservation<T> resolvedReservation = null;
            if (m_WaitingQueue.Count > 0)
            {
                while (m_WaitingQueue.Count > 0)
                {
                    var next = m_WaitingQueue.Dequeue();
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    if (resolvedReservation == null && !next.isDisposed && next.IsSuitable(freeRoom))
                    {
                        resolvedReservation = next;
                    }
                    m_ContinueWaitingQueue.Enqueue(next);
                }

                if (resolvedReservation != null)
                {
                    m_Rooms[freeRoom] = resolvedReservation;
                    m_ReservationsCount++;
                    resolvedReservation.CheckIn(freeRoom).Forget();
                }
                (m_ContinueWaitingQueue, m_WaitingQueue) = (m_WaitingQueue, m_ContinueWaitingQueue);
            }

            if (resolvedReservation == null)
            {
                /* Search for reservation that already has a room, but not preferred */
                foreach (var reserve in m_Rooms.ToArray())
                {
                    if (reserve.Value != null && reserve.Value != reservation && !reserve.Value.isDisposed && !reserve.Value.isSatisfied && reserve.Value.IsSuitable(freeRoom))
                    {
                        resolvedReservation = reserve.Value;
                        m_Rooms[freeRoom] = resolvedReservation;
                        m_ReservationsCount++;
                        resolvedReservation.CheckIn(freeRoom).Forget();
                    }
                }
            }
            
            if (m_ReservationsCount == 0)
            {
                OnEmpty?.Invoke();
            }
        }
        
        /// <summary>
        /// Try to resettle reservation to another room. Returns true if reservation was settled.
        /// It uses only preferred rooms to find a new room.
        /// </summary>
        /// <param name="reservation"></param>
        /// <returns></returns>
        public bool TryResettleReservation(MotelReservation<T> reservation)
        {
            var preferredRoomsArray = Bitmask.ExtractArray(reservation.preferredRooms);
            var settled = false;
            foreach (var room in preferredRoomsArray)
            {
                if (!room.Equals(reservation.room) && (!m_Rooms.ContainsKey(room) || m_Rooms[room] == null))
                {
                    m_Rooms[room] = reservation;
                    m_ReservationsCount++;
                    reservation.CheckIn(room).Forget();
                    settled = true;
                    break;
                }
            }

            return settled;
        }

        private void Abort()
        {
            m_AbortTokenSource.Cancel();
            m_ReleaseAllReservationsTask = ReleaseAllReservations();
        }
        
        private UniTask ReleaseAllReservations()
        {
            List<UniTask> tasks = new List<UniTask>();
            /* Dispose all reservations */
            foreach (var reservation in m_Rooms.Values.ToArray())
            {
                if (reservation != null)
                {
                    tasks.Add(reservation.DisposeAsync());
                }
            }
            /* Dispose all reservations from queue */
            foreach (var reservation in m_WaitingQueue)
            {
                tasks.Add(reservation.DisposeAsync());
            }
            
            
            
            OnDisposed?.Invoke();
            /* Wait for all reservations to be disposed */
            return UniTask.WhenAll(tasks);
        }

        public UniTask DisposeAsync()
        {
            if (m_IsDisposeProcessing)
            {
                throw new InvalidOperationException("Motel is already disposing");
            }
            m_IsDisposeProcessing = true;
            
            
            /* Wait for all reservations to be disposed */
            if (!m_ReleaseAllReservationsTask.HasValue)
            {
                m_ReleaseAllReservationsTask = ReleaseAllReservations();
            }
            
            OnDisposed?.Invoke();
            m_AbortTokenSource.Dispose();

            return m_ReleaseAllReservationsTask.Value;
        }

        public bool isDisposed => m_IsDisposeProcessing;
        public event Action OnDisposed;
    }
}