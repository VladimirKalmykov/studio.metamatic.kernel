using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public class Motel<T> : IUniTaskAsyncDisposable where T : Enum
    {
        private readonly Dictionary<T, MotelReservation<T>> m_Rooms;
        private Queue<MotelReservation<T>> m_WaitingQueue = new();
        private Queue<MotelReservation<T>> m_ContinueWaitingQueue = new();
        internal CancellationTokenSource m_AbortTokenSource = new();
        
        private IEnumerable<T> m_AllRooms;
        private bool m_IsDisposeProcessing;
        
        public CancellationToken abortToken => m_AbortTokenSource.Token;
        
        public Motel()
        {
            m_AllRooms = Enum.GetValues(typeof(T)).Cast<T>().Where(x => !x.Equals(default(T)));
            m_Rooms = new Dictionary<T, MotelReservation<T>>();
        }

        public MotelReservation<T> Reserve(T preferredRooms, Func<T, MotelReservation<T>, UniTask> onSettle,
            bool strict = false, object[] dependencies = null)
        {
            var reservation = new MotelReservation<T>(this, preferredRooms, onSettle, strict, dependencies);
            var preferredRoomsArray = Bitmask.ExtractArray(preferredRooms);
            var settled = false;
            foreach (var room in preferredRoomsArray)
            {
                if (!m_Rooms.ContainsKey(room) || m_Rooms[room] == null)
                {
                    m_Rooms[room] = reservation;
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
        
        internal void ReleaseReservation(MotelReservation<T> reservation)
        {
            var freeRoom = reservation.room;
            if (m_Rooms.TryGetValue(freeRoom, out var current) && current == reservation)
            {
                m_Rooms[freeRoom] = null;
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
                    resolvedReservation.CheckIn(freeRoom).Forget();
                }
                (m_ContinueWaitingQueue, m_WaitingQueue) = (m_WaitingQueue, m_ContinueWaitingQueue);
            }

            if (resolvedReservation == null)
            {
                /* Search for reservation that already has a room, but not preferred */
                foreach (var reserve in m_Rooms)
                {
                    if (reserve.Value != null && !reserve.Value.isSatisfied && !reserve.Value.IsSuitable(freeRoom))
                    {
                        resolvedReservation = reserve.Value;
                        m_Rooms[freeRoom] = resolvedReservation;
                        resolvedReservation.CheckIn(freeRoom).Forget();
                    }
                }
            }
        }
        
        internal void Abort()
        {
            m_AbortTokenSource.Cancel();
            m_AbortTokenSource.Dispose();
            m_AbortTokenSource = new CancellationTokenSource();
        }

        public UniTask DisposeAsync()
        {
            if (m_IsDisposeProcessing)
            {
                throw new InvalidOperationException("Motel is already disposing");
            }
            m_IsDisposeProcessing = true;
            List<UniTask> tasks = new List<UniTask>();
            /* Dispose all reservations */
            foreach (var reservation in m_Rooms.Values)
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
            /* Wait for all reservations to be disposed */
            return UniTask.WhenAll(tasks);
        }
    }
}