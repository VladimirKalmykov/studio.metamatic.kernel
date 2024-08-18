using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Action = System.Action;

namespace Metamatic
{
    public class MotelT<T>: IExposedDisposed, IUniTaskAsyncDisposableAbortable
    {
        private class Guest
        {
            public int Version;
            public object[] Dependencies;
            public MicroOperation Operation;
            public Func<T, InternalMicroOperation, UniTask> OnCheckIn;
            public T[] PreferredCells;
            public bool Strict;
            public bool IsCheckedIn;
            public T CellId = default;
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
                Dependencies = null;
                Operation = default;
                OnCheckIn = null;
                PreferredCells = null;
                Strict = false;
                IsCheckedIn = false;
                CellId = default;
                IsDisposed = false;
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
        
        private static readonly ParamLessPool<Guest> GuestPool = new ();
        
        private readonly Dictionary<T, int> m_Cells = new();
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
        
        public MotelT(int cellCapacity = 1, object[] dependencies = null)
        {
            m_Config_CellCapacity = cellCapacity;
            
            if (dependencies != null)
            {
                OnDisposed += ObserveExposedDisposable.Create(dependencies, Abort);
            }
        }

        public void AddCell(T cell)
        {
            m_Cells[cell] = 0;
        }
        
        
        public IUniTaskAsyncDisposable Reserve(T[] preferredCells, Func<T, InternalMicroOperation, UniTask> onCheckIn, bool strict = false, object[] dependencies = null)
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
                m_WaitingQueue.Add(guest);
            }
            else
            {
                CheckIn(guest, freeCellId);
            }
            
            return new AsyncDisposableFunc(async () =>
            {
                guest.IsDisposed = true;
                if (m_CheckedInUnsatisfiedGuests.Contains(guest))
                {
                    m_CheckedInUnsatisfiedGuests.Remove(guest);
                }
                UniTask asyncDisposeTask;
                if (guest.IsCheckedIn)
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

                await asyncDisposeTask;
                /* Remove guest from list */
                m_Guests.Remove(guest);
                /* Reset guest */
                guest.Reset();
                /* Return guest to pool */
                GuestPool.Return(guest);
            });
        }
        
        private void CheckIn(Guest guest, T cellId)
        {
            m_Cells[cellId]++;
            guest.CellId = cellId;
            guest.IsCheckedIn = true;
            var onCheckIn = guest.OnCheckIn;
            
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
            guest.Operation = MicroOperation.Run(Handler, dependencies: combinedDependencies);
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
                
                guest.CellId = default;
                guest.IsCheckedIn = false;
                guest.Operation = default;
                m_Cells[cellId]--;
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
        
        private void CheckInNextGuest(T cellId)
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
                    if (!guest.Strict || guest.IsSatisfied(cellId))
                    {
                        m_WaitingQueue.RemoveAt(i);
                        CheckIn(guest, cellId);
                        break;
                    }
                }
            }
        }

        private async UniTaskVoid RecheckInGuest(Guest guest, T cellId)
        {
            var version = guest.Version;
            m_Cells[cellId]++;
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
                if (version != guest.Version || isDisposed || guest.IsDisposed)
                {
                    m_Cells[cellId]--;
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
        /// <param name="cell"></param>
        /// <returns></returns>
        private bool GetFreeCell(T[] preferredCells, bool strict, out T cell)
        {
            foreach (var preferredCell in preferredCells)
            {
                if (m_Cells.TryGetValue(preferredCell, out var countInCell) && countInCell < m_Config_CellCapacity)
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
                if (c.Value < m_Config_CellCapacity)
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