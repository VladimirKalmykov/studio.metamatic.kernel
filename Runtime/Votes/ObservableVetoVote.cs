using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    /// <summary>
    /// Veto vote solves the problem of voting for a boolean value when only one opposite vote is important.
    /// For example, if the default value is false and there are 2 votes, 1 true and 1 false, the result will be true, because the false vote is vetoed.
    /// Use case: Voting for destroying an object. If one person votes against, the object will not be destroyed.
    /// </summary>
    public class ObservableVetoVote
    {
        private readonly bool m_Default;
        
        private int m_Cons;
        private Observable<bool> m_Result;
        public bool result => m_Result.Value;
        
        public ObservableVetoVote(bool @default)
        {
            m_Default = @default;
            m_Result = new Observable<bool>(@default);
        }

        public DisposableAction Vote(bool value)
        {
            if (!value.Equals(m_Default))
            {
                m_Cons++;
            }
            SummingUp();
            bool disposed = false;
            return new DisposableAction(() =>
            {
                if (disposed)
                {
                    return;
                }
                disposed = true;
                if (!value.Equals(m_Default))
                {
                    m_Cons--;
                }
                SummingUp();
            });
        }
        
        public DisposableAction Watch(Action<bool> action)
        {
            return m_Result.Watch(action);
        }
        
        public DisposableAction WatchAsync(Func<bool, Signal, UniTaskVoid> action)
        {
            return m_Result.WatchAsync(action);
        }
        
        private void SummingUp()
        {
            m_Result.SetUniqValue(m_Cons > 0 ? !m_Default : m_Default);
        }
    }
}