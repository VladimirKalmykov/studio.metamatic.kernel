using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    public class MotelReservation<T>: IUniTaskAsyncDisposable, IExposedDisposed where T : Enum
    {
        public enum Status
        {
            Waiting,
            CheckingIn,
            CheckedIn,
            CheckingOut,
            CheckedOut
        }
        public readonly Motel<T> Motel;
        
        #region User Preferences and Settings
        private readonly Func<T, MotelReservation<T>, UniTask> m_Handler;
        private readonly T m_Preference_PreferredRooms;
        private readonly bool m_Preference_Strict;
        #endregion
        
        #region Exposed Properties
        /// <summary>
        /// Current room.
        /// </summary>
        public T room { get; private set; }
        /// <summary>
        /// Next room (if the check-out resettle process initiated).
        /// </summary>
        public T nextRoom { get; private set; }
        /// <summary>
        /// Determines if the reservation is satisfied (current or next room is matched with the preferred rooms).
        /// </summary>
        public bool isSatisfied { get; private set; }
        /// <summary>
        /// CancellationToken for the check-in process (used the name cancellationToken to simplify the code, but for better understanding the name should be checkInCancellationToken).
        /// </summary>
        public CancellationToken cancellationToken => m_CheckInCancellationTokenSource.Token;
        /// <summary>
        /// CancellationToken for the check-out process.
        /// </summary>
        public CancellationToken checkOutCancellationToken => m_CheckOutCancellationTokenSource.Token;
        /// <summary>
        /// Determines if the check-out process is aborted (instead of async disposing the reservation, the check-out process should be aborted synchronously right now).
        /// </summary>
        public bool isCheckOutAborted => checkOutCancellationToken.IsCancellationRequested;
        #endregion
        
        #region Private Fields
        private Status m_Status = Status.Waiting;
        /// <summary>
        /// Cancellation token source for the check-in process.
        /// </summary>
        private CancellationTokenSource m_CheckInCancellationTokenSource;
        /// <summary>
        /// Cancellation token source for the check-out process.
        /// </summary>
        private CancellationTokenSource m_CheckOutCancellationTokenSource;

        private AutoResetUniTaskCompletionSource m_AsyncDisposingTaskCompletionSource;
        /// <summary>
        /// Contains current check-out task.
        /// </summary>
        private UniTask? m_CheckOutTask;
        private bool m_IsDisposed;
        private bool m_IsAborted;
        #endregion
       
        
        public MotelReservation(Motel<T> motel, T preferredRooms, Func<T, MotelReservation<T>, UniTask> handler, bool strict = false, object[] dependencies = null)
        {
            Motel = motel;
            m_CheckOutCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(Motel.abortToken);
            m_Handler = handler;
            m_Preference_PreferredRooms = preferredRooms;
            m_Preference_Strict = strict;
            if (dependencies != null)
            {
                OnDisposed += new ObserveExposedDisposable(dependencies, Abort);
            }
        }
        
        internal async UniTaskVoid CheckIn(T next)
        {
            if (m_IsDisposed)
            {
                throw new ObjectDisposedException(nameof(MotelReservation<T>));
            }

            if (m_IsAborted)
            {
                throw new InvalidOperationException("The reservation is aborted.");
            }
            isSatisfied = m_Preference_PreferredRooms.HasFlag(next);
            if (m_Status is Status.CheckingOut or Status.CheckedOut)
            {
                /* If the reservation is checking out, then we need to override the next room, thus after checked out it accept correct next room */
                nextRoom = next;
                return;
            }
            nextRoom = next;
            try
            {
                await CheckOut();
            }
            catch (Exception e)
            {
                Debug.LogError($"Check-out from room {room} process failed: {e}"); // TODO: Decide what to do with this error
            }

            if (m_IsDisposed || m_IsAborted) return;
    
            /* Create linked token source for the check-in process that will be cancelled if the abort token (checking out) cancelled */
            m_CheckInCancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(checkOutCancellationToken);
            m_Status = Status.CheckingIn;
            room = nextRoom;
            m_CheckOutTask = m_Handler(nextRoom, this);
            nextRoom = default;
            m_Status = Status.CheckedIn;
        }

        private async UniTask CheckOut(bool abort = false)
        {
            if (m_Status is Status.Waiting)
            {
                m_AsyncDisposingTaskCompletionSource = AutoResetUniTaskCompletionSource.Create();
                m_AsyncDisposingTaskCompletionSource.TrySetResult();
                return;
            }
            if (m_Status is Status.CheckedOut) return;
            if (m_Status is Status.CheckingOut)
            {
                if (abort)
                {
                    if (!m_CheckOutCancellationTokenSource.IsCancellationRequested)
                    {
                        m_CheckOutCancellationTokenSource.Cancel();
                    }
                    return;
                }
                else
                {
                    Debug.LogError("The reservation is already checking out.");
                    return;
                }
            }

            if (m_AsyncDisposingTaskCompletionSource != null)
            {
                m_AsyncDisposingTaskCompletionSource.Task.Forget();
            }
            m_AsyncDisposingTaskCompletionSource = AutoResetUniTaskCompletionSource.Create();
            m_Status = Status.CheckingOut;
            if (abort)
            {
                m_CheckOutCancellationTokenSource.Cancel();
            }
            m_CheckInCancellationTokenSource.Cancel();
            
            if (m_CheckOutTask.HasValue)
            {
                try
                {
                    await m_CheckOutTask.Value;
                    m_AsyncDisposingTaskCompletionSource.TrySetResult();
                }
                catch (OperationCanceledException ex)
                {
                    // Ignore
                    m_AsyncDisposingTaskCompletionSource.TrySetCanceled();
                }
                catch (Exception e)
                {
                    m_AsyncDisposingTaskCompletionSource.TrySetException(e);
                }
                finally
                {
                    m_CheckOutTask = null;
                }
            }
            m_CheckInCancellationTokenSource?.Dispose();
            m_Status = Status.CheckedOut;
        }
        
        /// <summary>
        /// If a room is suitable for the reservation according to its preferences.
        /// </summary>
        /// <param name="someRoom"></param>
        /// <returns></returns>
        public bool IsSuitable(T someRoom)
        {
            return m_Preference_PreferredRooms.HasFlag(someRoom);
        }
        
        private void Abort()
        {
            m_IsAborted = true;
            CheckOut(true).Forget();
            Motel.ReleaseReservation(this);
        }

        public async UniTask DisposeAsync()
        {
            if (m_IsDisposed)
            {
                return;
            }
            m_IsDisposed = true;
            nextRoom = default;
            if (m_Status is not Status.CheckedOut && m_Status is not Status.CheckingOut)
            {
                await CheckOut();
            } else if (m_AsyncDisposingTaskCompletionSource != null)
            {
                try
                {
                    await m_AsyncDisposingTaskCompletionSource.Task;
                } catch (OperationCanceledException)
                {
                    // Ignore
                }
                finally
                {
                    m_AsyncDisposingTaskCompletionSource = null;
                }
            }
            m_CheckInCancellationTokenSource?.Dispose();
            m_CheckOutCancellationTokenSource?.Dispose();
            if (!m_IsAborted)
            {
                Motel.ReleaseReservation(this);
            }
            OnDisposed?.Invoke();
        }

        public bool isDisposed => m_IsDisposed;
        public event Action OnDisposed;
    }
}