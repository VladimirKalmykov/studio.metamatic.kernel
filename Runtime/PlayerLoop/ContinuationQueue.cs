using System;
using System.Threading;

namespace Metamatic
{
    /// <summary>
    /// Create a queue of actions that will be executed when Run is called or when the queue is disposed.
    /// To get know when the queue is disposed, check the IsDisposed property.
    /// </summary>
    public sealed class ContinuationQueue: IDisposable
    {
        const int MaxArrayLength = 0X7FEFFFFF;
        const int InitialSize = 16;

        private SpinLock m_Gate = new SpinLock(false);
        private bool m_Dequing = false;

        private int m_ActionListCount = 0;
        private Action[] m_ActionList = new Action[InitialSize];

        int m_WaitingListCount = 0;
        private Action[] m_WaitingList = new Action[InitialSize];

        public bool IsDisposed = false;

        public void Enqueue(Action continuation)
        {
            bool lockTaken = false;
            try
            {
                m_Gate.Enter(ref lockTaken);

                if (m_Dequing)
                {
                    // Ensure Capacity
                    if (m_WaitingList.Length == m_WaitingListCount)
                    {
                        var newLength = m_WaitingListCount * 2;
                        if ((uint)newLength > MaxArrayLength) newLength = MaxArrayLength;

                        var newArray = new Action[newLength];
                        Array.Copy(m_WaitingList, newArray, m_WaitingListCount);
                        m_WaitingList = newArray;
                    }
                    m_WaitingList[m_WaitingListCount] = continuation;
                    m_WaitingListCount++;
                }
                else
                {
                    // Ensure Capacity
                    if (m_ActionList.Length == m_ActionListCount)
                    {
                        var newLength = m_ActionListCount * 2;
                        if ((uint)newLength > MaxArrayLength) newLength = MaxArrayLength;

                        var newArray = new Action[newLength];
                        Array.Copy(m_ActionList, newArray, m_ActionListCount);
                        m_ActionList = newArray;
                    }
                    m_ActionList[m_ActionListCount] = continuation;
                    m_ActionListCount++;
                }
            }
            finally
            {
                if (lockTaken) m_Gate.Exit(false);
            }
        }

        public int Clear()
        {
            var rest = m_ActionListCount + m_WaitingListCount;

            m_ActionListCount = 0;
            m_ActionList = new Action[InitialSize];

            m_WaitingListCount = 0;
            m_WaitingList = new Action[InitialSize];

            return rest;
        }

        [System.Diagnostics.DebuggerHidden]
        public void Run()
        {
            {
                bool lockTaken = false;
                try
                {
                    m_Gate.Enter(ref lockTaken);
                    if (m_ActionListCount == 0) return;
                    m_Dequing = true;
                }
                finally
                {
                    if (lockTaken) m_Gate.Exit(false);
                }
            }

            for (int i = 0; i < m_ActionListCount; i++)
            {

                var action = m_ActionList[i];
                m_ActionList[i] = null;
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }

            {
                bool lockTaken = false;
                try
                {
                    m_Gate.Enter(ref lockTaken);
                    m_Dequing = false;

                    var swapTempActionList = m_ActionList;

                    m_ActionListCount = m_WaitingListCount;
                    m_ActionList = m_WaitingList;

                    m_WaitingListCount = 0;
                    m_WaitingList = swapTempActionList;
                }
                finally
                {
                    if (lockTaken) m_Gate.Exit(false);
                }
            }
        }

        public void Dispose()
        {
            IsDisposed = true;
            Run();
            /* Once run, the queue is disposed */
            m_ActionList = null;
            m_WaitingList = null;
        }
    }
}