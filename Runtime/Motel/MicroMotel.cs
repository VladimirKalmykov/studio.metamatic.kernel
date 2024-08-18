using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Action = System.Action;

namespace Metamatic
{
    public class MicroMotel: IExposedDisposed, IExposedAsyncDisposed, IUniTaskAsyncDisposable
    {
        private enum CellStatus
        {
            Busy,
            Free
        }
        
        private class Guest
        {
            public object[] Dependencies;
            public MicroOperation Operation;
            public Func<int, InternalMicroOperation, UniTask> OnCheckIn;
            public int[] PreferredCells;
            public bool Strict;
            public int CellId = -1;
            /// <summary>
            /// This flag is set to true when the guest is disposed by outside.
            /// </summary>
            public bool IsDisposed;

            public void Reset()
            {
                Dependencies = null;
                Operation = default;
                OnCheckIn = null;
                PreferredCells = null;
                Strict = false;
                CellId = -1;
                IsDisposed = false;
            }
            
            public bool IsSatisfied(int cellId)
            {
                if (cellId == -1) return false;
                if (PreferredCells == null || PreferredCells.Length == 0)
                {
                    return true;
                }

                foreach (var preferredCell in PreferredCells)
                {
                    if (preferredCell == cellId)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
        
        private static readonly ParamLessPool<Guest> GuestPool = new ();
        
        private readonly Dictionary<int, CellStatus> m_Cells = new();
        private readonly List<Guest> m_Guests = new();
        /// <summary>
        /// List of checked in guests (that already have a cell).
        /// </summary>
        private readonly List<Guest> m_CheckedInUnsatisfiedGuests = new();
        /// <summary>
        /// Queue of guests waiting for a cell.
        /// </summary>
        private readonly List<Guest> m_WaitingQueue = new();
        
        public MicroMotel(int cellsCount)
        {
            for (var i = 0; i < cellsCount; i++)
            {
                m_Cells.Add(i, CellStatus.Free);
            }
        }
        
        
        public IUniTaskAsyncDisposable Reserve(int[] preferredCells, Func<int, InternalMicroOperation, UniTask> onCheckIn, bool strict = false, object[] dependencies = null)
        {
            /*
             * Create guest
             */
            var guest = GuestPool.Get();
            guest.Dependencies = dependencies;
            guest.OnCheckIn = onCheckIn;
            guest.PreferredCells = preferredCells;
            guest.Strict = strict;

            var freeCellId = GetFreeCell(preferredCells, strict);
            
            m_Guests.Add(guest);
            
            if (freeCellId == -1)
            {
                m_WaitingQueue.Add(guest);
            }
            else
            {
                CheckIn(guest, freeCellId);
            }
            
            return new AsyncDisposableFunc(() =>
            {
                guest.IsDisposed = true;
                if (m_CheckedInUnsatisfiedGuests.Contains(guest))
                {
                    m_CheckedInUnsatisfiedGuests.Remove(guest);
                }
                UniTask asyncDisposeTask = default;
                if (guest.CellId != -1)
                {
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
                }
                else
                {
                    asyncDisposeTask = UniTask.CompletedTask;
                    m_WaitingQueue.Remove(guest);
                }
                /* Remove guest from list */
                m_Guests.Remove(guest);
                /* Reset guest */
                guest.Reset();
                /* Return guest to pool */
                GuestPool.Return(guest);

                return asyncDisposeTask;
            });
        }
        
        private void CheckIn(Guest guest, int cellId)
        {
            m_Cells[cellId] = CellStatus.Busy;
            guest.CellId = cellId;
            var onCheckIn = guest.OnCheckIn;
            
            UniTask Handler(InternalMicroOperation op)
            {
                return onCheckIn(cellId, op);
            }
            
            guest.Operation = MicroOperation.Run(Handler, dependencies: guest.Dependencies);;
            if (!guest.IsSatisfied(cellId))
            {
                m_CheckedInUnsatisfiedGuests.Add(guest);
            }
            guest.Operation.OnDisposedAsync += exception =>
            {
                if (exception != null)
                {
                    Debug.LogError($"Error was thrown during guest operation disposing: {exception}");
                }

                if (isDisposedAsync)
                {
                    /* Exit if motel is completely disposed */
                    return;
                }
                
                guest.CellId = -1;
                guest.Operation = default;
                m_Cells[cellId] = CellStatus.Free;
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
        
        private void CheckInNextGuest(int cellId)
        {
            lock (m_Cells)
            {
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
                    
                            return;
                        }
                    }
                }
            
                for (var i = 0; i < m_WaitingQueue.Count; i++)
                {
                    var guest = m_WaitingQueue[i];
                    if (guest.IsSatisfied(cellId))
                    {
                        m_WaitingQueue.RemoveAt(i);
                        CheckIn(guest, cellId);
                        break;
                    }
                }
            }
        }

        private async UniTaskVoid RecheckInGuest(Guest guest, int cellId)
        {
            m_Cells[cellId] = CellStatus.Busy;
            /* Check out guest */
            try
            {
                await guest.Operation.DisposeAsync();
            }
            catch
            {
                Debug.LogError("Error was thrown during guest operation disposing");
            }
            finally
            {
                if (isDisposed || guest.IsDisposed)
                {
                    m_Cells[cellId] = CellStatus.Free;
                }
                else
                {
                    CheckIn(guest, cellId);
                }
            }
        }
        
        /// <summary>
        /// Get free cell for specified preferred cells and strict mode.
        /// </summary>
        /// <param name="preferredCells"></param>
        /// <param name="strict"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private int GetFreeCell(int[] preferredCells, bool strict)
        {
            foreach (var preferredCell in preferredCells)
            {
                if (m_Cells.TryGetValue(preferredCell, out var cellStatus) && cellStatus == CellStatus.Free)
                {
                    return preferredCell;
                }
            }

            if (strict)
            {
                return -1;
            }

            foreach (var cell in m_Cells)
            {
                if (cell.Value == CellStatus.Free)
                {
                    return cell.Key;
                }
            }

            return -1;
        }
        
        private bool IsMatchCell(int[] preferredCells, int cell)
        {
            foreach (var preferredCell in preferredCells)
            {
                if (preferredCell == cell)
                {
                    return true;
                }
            }

            return false;
        }

        private bool m_IsDisposed;
        private bool m_IsAsyncDisposed;
            
        public bool isDisposed => m_IsDisposed;
        public bool isDisposedAsync => m_IsAsyncDisposed;
            
        public event Action OnDisposed;
        public event Action<Exception> OnDisposedAsync;
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
                  await UniTask.WhenAll(tasks);
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
    }
}