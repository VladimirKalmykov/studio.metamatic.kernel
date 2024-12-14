using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Action = System.Action;

namespace Metamatic
{
    public class MotelT<T>: IExposedDisposed, IUniTaskAsyncDisposableAbortable
    {
        private class Cell
        {
            public int Version;
            public int Capacity;
            public int Count;
            public bool IsDisposed;
            
            public void Reset()
            {
                unchecked
                {
                    Version++;
                }
                Capacity = 0;
                Count = 0;
                IsDisposed = false;
            }
        }
        internal class Guest
        {
            public enum CheckStatus
            {
                Idle,
                Waiting,
                CheckedIn,
                CheckingOut
            }
            public string Id = Guid.NewGuid().ToString();
            public int Version;
            public int ProcessVersion;
            public object[] Dependencies;
            public AsyncDisposableOperation Operation;
            public Func<T, InternalMicroOperation, UniTask> OnCheckIn;
            public T[] PreferredCells;
            public bool Strict;
            public T CellId = default;
            public CheckStatus Status;
            /// <summary>
            /// This flag is set to true when the guest is disposed by outside.
            /// </summary>
            public bool IsDisposed;

            public void Reset()
            {
                unchecked
                {
                    Version++;
                }
                ProcessVersion = 0;
                Dependencies = null;
                Operation = default;
                OnCheckIn = null;
                PreferredCells = null;
                Strict = false;
                CellId = default;
                IsDisposed = false;
                Status = CheckStatus.Idle;
            }
            
            public bool IsSatisfied(T cell)
            {
                if (PreferredCells == null || PreferredCells.Length == 0)
                {
                    return true;
                }

                foreach (var preferredCell in PreferredCells)
                {
                    if (preferredCell.Equals(cell))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public readonly struct Reservation: IUniTaskAsyncDisposable
        {
            private readonly IUniTaskAsyncDisposable m_Operation;
            private readonly MotelT<T> m_Motel;
            private readonly Guest m_Guest;
            // ReSharper disable once InconsistentNaming
            public string Id => m_Guest.Id;
            
            internal Reservation(IUniTaskAsyncDisposable operation, MotelT<T> motel, Guest guest)
            {
                m_Operation = operation;
                m_Motel = motel;
                m_Guest = guest;
            }
            
            public void SetPreferredCells(T[] preferredCells, bool strict = false)
            {
                m_Motel.ChangeGuestPreferredPosition(m_Guest, preferredCells, strict);
            }
            
            public void SetPreferredCells(T preferredCell, bool strict = false)
            {
                m_Motel.ChangeGuestPreferredPosition(m_Guest, new [] { preferredCell }, strict);
            }

            public UniTask DisposeAsync()
            {
                return m_Operation.DisposeAsync();
            }
        }
        
        private static readonly ParamLessPool<Guest> GuestPool = new ();
        private static readonly ParamLessPool<Cell> CellPool = new ();
        
        private readonly Dictionary<T, Cell> m_Cells = new();
        private readonly List<Guest> m_Guests = new();
        /// <summary>
        /// List of checked in guests (that already have a cell).
        /// </summary>
        private readonly List<Guest> m_CheckedInUnsatisfiedGuests = new();
        /// <summary>
        /// Queue of guests waiting for a cell.
        /// </summary>
        private readonly List<Guest> m_WaitingQueue = new();
        private readonly int m_Config_CellCapacity;
        private int m_CellsVersion;
        
        #region Public events
        /// <summary>
        /// The event is called when the motel is empty (no guests).
        /// </summary>
        public event Action OnEmpty;
        #endregion
        
        public MotelT(int cellCapacity = 1, object[] dependencies = null)
        {
            m_Config_CellCapacity = cellCapacity;
            
            if (dependencies != null)
            {
                OnDisposed += ObserveExposedDisposable.Create(dependencies, Abort);
            }
        }

        public void AddCell(T cell, int capacity = 0)
        {
            Debug.Assert(capacity > -1, "Capacity should be greater or equal to 0");
            var normalizedCapacity = capacity == 0 ? m_Config_CellCapacity : capacity;
            if (m_Cells.ContainsKey(cell))
            {
                var existingCell = m_Cells[cell];
                existingCell.IsDisposed = false; // Cancel disposing
                ChangeCellCapacity(cell, normalizedCapacity).Forget();
                return;
            }
            var newCell = CellPool.Get();
            newCell.Capacity = normalizedCapacity;
            
            /* Increase cells versions that will be used for guests that still in process recheck after complete processing */
            unchecked
            {
                m_CellsVersion++;
            }
            if (!m_Cells.TryAdd(cell, newCell))
            {
                throw new Exception($"Cell {cell} already exists");
            }

            CheckInNextGuest(cell, normalizedCapacity);
        }

        private Action m_ChangeCellQueue;
        private bool m_IsBeingChanged;
        
        
        private void ChangeGuestPreferredPosition(Guest guest, T[] preferredCells, bool strict = false)
        {
            guest.PreferredCells = preferredCells;
            guest.Strict = strict;
            Recheck(guest).Forget();
        }
        
        public async UniTaskVoid ChangeCellCapacity(T cell, int capacity)
        {
            Debug.Assert(capacity > -1, "Capacity should be greater or equal to 0");
            if (m_IsBeingChanged)
            {
                m_ChangeCellQueue += () => ChangeCellCapacity(cell, capacity).Forget();
            }
            m_IsBeingChanged = true;
            if (m_Cells.TryGetValue(cell, out var cellData))
            {
                cellData.Capacity = capacity;
                if (cellData.Count > capacity)
                {
                    var countToEvict = cellData.Count - capacity;
                    var evictionTasks = new List<UniTask>();
                    foreach (var guest in m_Guests)
                    {
                        if (countToEvict == 0) break;
                        if (guest.Status == Guest.CheckStatus.CheckedIn && guest.CellId.Equals(cell))
                        {
                            evictionTasks.Add(Eviction(guest));
                            countToEvict--;
                        }
                    }

                    try
                    {
                        await UniTask.WhenAll(evictionTasks);    
                    } catch (Exception e)
                    {
                        Debug.LogError($"Error was thrown during eviction: {e}");
                    }
                } else if (cellData.Count < capacity)
                {
                    var countToCheckIn = capacity - cellData.Count;
                    CheckInNextGuest(cell, countToCheckIn);
                }
            }
            unchecked
            {
                m_CellsVersion++;
            }
            m_IsBeingChanged = false;
            
            if (m_ChangeCellQueue != null)
            {
                m_ChangeCellQueue();
                m_ChangeCellQueue = null;
            }
        }
        
        public async UniTask RemoveCell(T cell)
        {
            if (m_Cells.TryGetValue(cell, out var cellData))
            {
                cellData.IsDisposed = true;
                if (cellData.Count > 0)
                {
                    var evictionTasks = new List<UniTask>();
                    foreach (var guest in m_Guests)
                    {
                        if (guest.Status == Guest.CheckStatus.CheckedIn && guest.CellId.Equals(cell))
                        {
                            evictionTasks.Add(Eviction(guest));
                        }
                    }

                    try
                    {
                        await UniTask.WhenAll(evictionTasks);    
                    } catch (Exception e)
                    {
                        Debug.LogError($"Error was thrown during eviction: {e}");
                    }
                }

                if (!cellData.IsDisposed)
                {
                    /* Removing cell is cancelled */
                    return;
                }
                m_Cells.Remove(cell);
                cellData.Reset();
                CellPool.Return(cellData);
                unchecked
                {
                    m_CellsVersion++;
                }
            }
        }
        
        
        public Reservation Reserve(T[] preferredCells, Func<T, InternalMicroOperation, UniTask> onCheckIn, bool strict = false, object[] dependencies = null)
        {
            /*
             * Create guest
             */
            var guest = GuestPool.Get();
            guest.Dependencies = dependencies;
            guest.OnCheckIn = onCheckIn;
            guest.PreferredCells = preferredCells;
            guest.Strict = strict;
            
            m_Guests.Add(guest);
            
            if (!GetFreeCell(preferredCells, strict, out var freeCellId))
            {
                guest.Status = Guest.CheckStatus.Waiting;
                m_WaitingQueue.Add(guest);
            }
            else
            {
                CheckIn(guest, freeCellId);
            }
            
            var operation = new AsyncDisposableFunc(async () =>
            {
                guest.IsDisposed = true;
                if (m_CheckedInUnsatisfiedGuests.Contains(guest))
                {
                    m_CheckedInUnsatisfiedGuests.Remove(guest);
                }
                UniTask asyncDisposeTask;
                if (guest.Status == Guest.CheckStatus.CheckedIn || guest.Status == Guest.CheckStatus.CheckingOut)
                {

                    asyncDisposeTask = SoftCheckOut(guest);
                    #if DEBUG
                    Debug.Assert(!m_WaitingQueue.Contains(guest), $"Waiting queue should not contain guest {guest.Id}");
                    #endif
                }
                else
                {
                    asyncDisposeTask = UniTask.CompletedTask;
                    guest.Status = Guest.CheckStatus.Idle;
                    m_WaitingQueue.Remove(guest);
                }

                try
                {
                    var competitiveTask = UniTask.Delay(5000);
                    var result = await UniTask.WhenAny(asyncDisposeTask, competitiveTask);
                    if (result == 1)
                    {
                        throw new Exception("Guest was not disposed in time");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"<color=red>[HINT]</color> Error: {e}");
                }
                finally
                {
                    
                    /* Remove guest from list */
                    m_Guests.Remove(guest);
                    /* Check for empty motel */
                    if (m_Guests.Count == 0)
                    {
                        OnEmpty?.Invoke();
                    }
#if DEBUG
                    var memoizedGuestId = guest.Id;
#endif
                    
                    /* Reset guest */
                    guest.Reset();
                    /* Return guest to pool */
                    GuestPool.Return(guest);
                }
            });
            
            return new Reservation(operation, this, guest);
        }
        
        private void CheckIn(Guest guest, T cellId)
        {
            m_Cells[cellId].Count++;
            guest.CellId = cellId;
            guest.Status = Guest.CheckStatus.CheckedIn;
            var guestVersion = guest.Version;
            var onCheckIn = guest.OnCheckIn;
            Debug.Assert(onCheckIn != null && !onCheckIn.Equals(null), $"[DEBUG-HINT] onCheckIn is null before handling (guestId: {guest.Id})");
            
            UniTask Handler(InternalMicroOperation op)
            {
                return onCheckIn(cellId, op);
            }
            
            var combinedDependencies = guest.Dependencies != null ? new object[guest.Dependencies.Length + 1] : new object[1];
            combinedDependencies[0] = this;
            if (guest.Dependencies != null)
            {
                Array.Copy(guest.Dependencies, 0, combinedDependencies, 1, guest.Dependencies.Length);
            }
            guest.Operation = AsyncDisposableOperation.Run(Handler, dependencies: combinedDependencies);
            if (!guest.IsSatisfied(cellId))
            {
                m_CheckedInUnsatisfiedGuests.Add(guest);
            }
            guest.Operation.OnDisposedAsync += exception =>
            {
                if (guest.Version != guestVersion)
                {
                    Debug.LogError("Guest version mismatch. It seems that guest was disposed and returned to pool, but operation was not disposed");
                }
                if (exception != null)
                {
                    Debug.LogError($"Error was thrown during guest operation disposing: {exception}");
                }

                if (isDisposedAsync)
                {
                    /* Exit if motel is completely disposed */
                    return;
                }
                
                guest.CellId = default;
                guest.Status = Guest.CheckStatus.Idle;
                guest.Operation = default;
                m_Cells[cellId].Count--;
                if (m_CheckedInUnsatisfiedGuests.Contains(guest))
                {
                    m_CheckedInUnsatisfiedGuests.Remove(guest);
                }
                if (!isDisposed)
                {
                    /* Check in next guest if motel is not disposed */
                    CheckInNextGuest(cellId);
                }
            };
        }
    
        /// <summary>
        /// Find more suitable cell for the guest.
        /// </summary>
        /// <param name="guest"></param>
        private async UniTask Recheck(Guest guest)
        {
            if (guest.Status == Guest.CheckStatus.CheckedIn)
            {
                if (guest.IsSatisfied(guest.CellId)) return; // Guest is already satisfied of current cell
                if (!GetFreeCell(guest.PreferredCells, guest.Strict, out var freeCellId))
                {
                    /* No suitable cell found */
                    return;
                }
                else
                {
                    await RecheckInGuest(guest, freeCellId);
                }
            }
            else if (guest.Status == Guest.CheckStatus.CheckingOut)
            {
                /*
                 * Guest is already checking out for some reason. It can be because of Eviction or RecheckinGuest, or Disposing guest.
                 * Thus first we need to check if guest is being disposed. If so, then we cancel recheck as well.
                 * If not, we need to cancel Eviction or RecheckInGuest, because it may have wrong cell calculation.
                 */
                unchecked
                {
                    guest.ProcessVersion++;
                }
                var memoizedProcessVersion = guest.ProcessVersion;
                /* Now we should wait until check out is completed */
                await SoftCheckOut(guest);
                /* Add guest to waiting queue */
                if (memoizedProcessVersion != guest.ProcessVersion)
                {
                    /* Some other process was started, so we need to break */
                }
                else
                {
                    if (!GetFreeCell(guest.PreferredCells, guest.Strict, out var freeCellId))
                    {
                        /* No suitable cell found. Add user to waiting queue */
                        guest.Status = Guest.CheckStatus.Waiting;
                        m_WaitingQueue.Add(guest);
                        return;
                    }
                    else
                    {
                        /* Check in guest */
                        CheckIn(guest, freeCellId);
                    }
                }
            } else if (guest.Status == Guest.CheckStatus.Waiting)
            {
                /*
                 * Guest in a waiting queue. We need to check if there is a suitable cell for it.
                 */
                if (!GetFreeCell(guest.PreferredCells, guest.Strict, out var freeCellId))
                {
                    /* No suitable cell found. Add user to waiting queue */
                    return;
                }
                else
                {
                    /* Remove from waiting queue */
                    m_WaitingQueue.Remove(guest);
                    guest.Status = Guest.CheckStatus.Idle;
                    /* Check in guest */
                    CheckIn(guest, freeCellId);
                }
            }
        }

        /// <summary>
        /// Forced eviction of a guest from an occupied cell. Unlike the checking out, this method additionally searches for another suitable cell for the guest
        /// The result of the eviction is that guest left current cell and either checked in to another cell or added to waiting queue.
        /// </summary>
        /// <param name="guest"></param>
        private async UniTask Eviction(Guest guest)
        {
            /* Get guest cell */
            if (!(guest.Status == Guest.CheckStatus.CheckedIn || guest.Status == Guest.CheckStatus.CheckingOut))
            {
                return;
            };
            var cellsVersions = m_CellsVersion;
            /* Find free cell */
            if (!GetFreeCell(guest.PreferredCells, guest.Strict, out var freeCellId))
            {
                unchecked
                {
                    guest.ProcessVersion++;
                }
                var memoizedProcessVersion = guest.ProcessVersion;
                try
                {
                    /* Just checkout guest */
                    await SoftCheckOut(guest);
                } catch (Exception e)
                {
                    Debug.LogError($"Error was thrown during eviction: {e}");
                }
                finally
                {
                    if (memoizedProcessVersion != guest.ProcessVersion)
                    {
                        /* Some other process was started, so we need to break */
                    }
                    else
                    {
                        /* And add it to waiting queue */
                        guest.Status = Guest.CheckStatus.Waiting;
                        m_WaitingQueue.Add(guest);
                        if (cellsVersions != m_CellsVersion)
                        {
                            /* If cells were changed during eviction, then we need to recheck guest */
                            Recheck(guest).Forget();
                        }
                    }
                }
            }
            else
            {
                await RecheckInGuest(guest, freeCellId);
            }
        }
        
        /// <summary>
        /// Checkout guest from motel. Support checkouting when guest is not checked in, and also if guest is being checkout. Simply put, this method guaranties that guest will be checked out.
        /// </summary>
        /// <param name="guest"></param>
        /// <returns></returns>
        private async UniTask SoftCheckOut(Guest guest)
        {
            
            if (!(guest.Status == Guest.CheckStatus.CheckedIn || guest.Status == Guest.CheckStatus.CheckingOut))
            {
                return;
            }
            
            guest.Status = Guest.CheckStatus.CheckingOut;
            UniTask asyncDisposeTask;
            
            if (!guest.Operation.isDisposed)
            {
                asyncDisposeTask = guest.Operation.DisposeAsync();
            }
            else
            {
                var tsc = AutoResetUniTaskCompletionSource.Create();
                guest.Operation.OnDisposedAsync += exception =>
                {
                    if (exception != null)
                    {
                        tsc.TrySetException(exception);
                    }
                    else
                    {
                        tsc.TrySetResult();
                    }
                };
                asyncDisposeTask = tsc.Task;
            }
            
            await asyncDisposeTask;
            guest.Status = Guest.CheckStatus.Idle;
            /* We don't need to decrement cell count here, because it will be decremented in OnDisposedAsync event */
        }
        
        private void CheckInNextGuest(T cellId, int countToCheckIn = 1)
        {
            lock (m_Cells)
            {
                while (countToCheckIn > 0)
                {
                    var guestCheckedIn = false;
                    lock (m_CheckedInUnsatisfiedGuests)
                    {
                        /* First, look for guests that are already checked in and unsatisfied */
                        for (var i = 0; i < m_CheckedInUnsatisfiedGuests.Count; i++)
                        {
                            var guest = m_CheckedInUnsatisfiedGuests[i];
                            if (!guest.IsDisposed && guest.IsSatisfied(cellId))
                            {
                                m_CheckedInUnsatisfiedGuests.RemoveAt(i);
                                RecheckInGuest(guest, cellId).Forget();
                                
                                guestCheckedIn = true;
                                --countToCheckIn;
                                break;
                            }
                        }
                    }

                    if (!guestCheckedIn)
                    {
                        for (var i = 0; i < m_WaitingQueue.Count; i++)
                        {
                            var guest = m_WaitingQueue[i];
                            if (!guest.Strict || guest.IsSatisfied(cellId))
                            {
                                guest.Status = Guest.CheckStatus.Idle;
                                m_WaitingQueue.RemoveAt(i);
                                CheckIn(guest, cellId);
                                --countToCheckIn;
                                break;
                            }
                        }
                    }
                    if (!guestCheckedIn)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Move guest to another cell.
        /// </summary>
        /// <param name="guest"></param>
        /// <param name="cellId"></param>
        private async UniTask RecheckInGuest(Guest guest, T cellId)
        {
            var startCellData = m_Cells[cellId];
            if (startCellData == null || startCellData.IsDisposed)
            {
                /* Eviction guest again */
                Eviction(guest).Forget();
                return;
            }
            var version = guest.Version;
            var cellDataVersion = startCellData.Version;
            startCellData.Count++;
            /* Check out guest */
            unchecked
            {
                guest.ProcessVersion++;
            }
            var memoizedProcessVersion = guest.ProcessVersion;
            var cellsVersions = m_CellsVersion;
            try
            {
                await SoftCheckOut(guest);
            }
            catch
            {
                Debug.LogError("Error was thrown during guest operation disposing");
            }
            finally
            {
                if (memoizedProcessVersion != guest.ProcessVersion)
                {
                    /* Some other process was started, so we need to break */
                    startCellData.Count--;
                }
                else
                {
                    var cellData = m_Cells[cellId];
                    if (cellData == null || cellData.IsDisposed || cellData.Version != cellDataVersion ||
                        version != guest.Version || isDisposed || guest.IsDisposed)
                    {
                        if (cellData != null && cellData.Version == cellDataVersion)
                        {
                            cellData.Count--;
                        }

                        /* If guest is still here: not disposed, motel is not disposed and guest has the same version, then eviction it again */
                        if (version == guest.Version && !isDisposed && !guest.IsDisposed)
                        {
                            /* Eviction guest again */
                            Eviction(guest).Forget();
                        }
                    }
                    else
                    {
                        cellData.Count--;
                        if (cellsVersions != m_CellsVersion)
                        {
                            /* If cells were changed during eviction, then we need to recheck guest */
                            Recheck(guest).Forget();
                        }
                        else
                        {
                            /* Check in guest */
                            CheckIn(guest, cellId);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get free cell for specified preferred cells and strict mode.
        /// </summary>
        /// <param name="preferredCells"></param>
        /// <param name="strict"></param>
        /// <param name="cell"></param>
        /// <returns></returns>
        private bool GetFreeCell(T[] preferredCells, bool strict, out T cell)
        {
            foreach (var preferredCell in preferredCells)
            {
                if (m_Cells.TryGetValue(preferredCell, out var cellData) && !cellData.IsDisposed && cellData.Count < cellData.Capacity)
                {
                    cell = preferredCell;
                    return true;
                }
            }

            if (strict)
            {
                cell = default;
                return false;
            }

            foreach (var c in m_Cells)
            {
                if (c.Value.Count < c.Value.Capacity)
                {
                    cell = c.Key;
                    return true;
                }
            }

            cell = default;
            return false;
        }

        private bool m_IsDisposed;
        private bool m_IsAsyncDisposed;
            
        public bool isDisposed => m_IsDisposed;
        public bool isDisposedAsync => m_IsAsyncDisposed;
            
        public event Action OnDisposed;
        public event Action<Exception> OnDisposedAsync;
        
        /// <summary>
        /// All guests operations have Motel instance as dependency, therefore, disposing Motel will abort all guests.
        /// </summary>
        public void Abort()
        {
            isAborted = true;
            OnAborted?.Invoke();
            DisposeAsync().Forget();
        }
        
        public async UniTask DisposeAsync()
        {
            if (m_IsDisposed)
            {
                return;
            }
            m_IsDisposed = true;
            OnDisposed?.Invoke();
              List<UniTask> tasks = new List<UniTask>();
              /* Dispose all guests */
              foreach (var guest in m_Guests)
              {
                  if (guest.Operation.isInitialized)
                  {
                      if (!guest.Operation.isDisposed)
                      {
                          tasks.Add(guest.Operation.DisposeAsync());
                      }
                      else
                      {
                          var tsc = AutoResetUniTaskCompletionSource.Create();
                          guest.Operation.OnDisposedAsync += exception =>
                          {
                              if (exception != null)
                              {
                                  tsc.TrySetException(exception);
                              }
                              else
                              {
                                  tsc.TrySetResult();
                              }
                          };
                          tasks.Add(tsc.Task);
                      }
                  }
              }

              try
              {
                  if (!isAborted)
                  {
                      await UniTask.WhenAll(tasks);
                  }
                  else
                  {
                      UniTask.WhenAll(tasks).Forget();
                  }
              } catch (Exception e)
              {
                  Debug.LogError($"Error was thrown during disposing: {e}");
                  m_IsAsyncDisposed = true;
                  OnDisposedAsync?.Invoke(e);
              }

              if (!m_IsAsyncDisposed)
              {
                    m_IsAsyncDisposed = true;
                    OnDisposedAsync?.Invoke(null);
              }
        }

        #region IUniTaskAsyncDisposableAbortable
        public bool isAborted { get; private set;  }
        public event Action OnAborted;
        #endregion
    }
}