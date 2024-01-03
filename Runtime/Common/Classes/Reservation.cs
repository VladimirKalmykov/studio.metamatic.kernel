using System;
using System.Collections.Generic;

namespace Metamatic
{
    public interface IReserved
    {
        public bool isReserved { get; }
        public Action Acquire(object booker);
        public void Dispose(object booker);
    }
    public class Reservation
    {
        public readonly object booker;
        public Reservation(object booker)
        {
            this.booker = booker;
        }
    }

    public class Reservations
    {
        private HashSet<Reservation> m_Reservations = new HashSet<Reservation>();
        private int m_AnonyousReservations;
        public bool isReserved => m_Reservations.Count > 0 || m_AnonyousReservations > 0;
        public System.Action Acquire(object booker)
        {
            #if DEBUG
            foreach (var res in m_Reservations)
            {
                if (res.booker == booker)
                {
                    throw new Exception("Booker already reserved");
                }
            }
            #endif
            var reservation = new Reservation(booker);
            m_Reservations.Add(reservation);
            return () =>
            {
                m_Reservations.Remove(reservation);
            };
        }

        /// <summary>
        /// Anonymously acquire reservation
        /// </summary>
        public void Acquire()
        {
            m_AnonyousReservations++;
        }
        
        /// <summary>
        /// Release all reservations for specific booker
        /// </summary>
        /// <param name="booker"></param>
        public void Release(object booker)
        {
            var toRemove = new List<Reservation>();
            foreach (var reservation in m_Reservations)
            {
                if (reservation.booker == booker)
                {
                    toRemove.Add(reservation);
                }
            }

            foreach (var reservation in toRemove)
            {
                m_Reservations.Remove(reservation);
            }
        }

        public void Release()
        {
            m_AnonyousReservations--;
        }
    }
}