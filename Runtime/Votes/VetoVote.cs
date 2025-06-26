using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    /// <summary>
    /// Veto vote solves the problem of voting for a boolean value when only one opposite vote is important.
    /// For example, if the default value is false and there are 2 votes, 1 true and 1 false, the result will be true, because the false vote is vetoed.
    /// Use case: Voting for destroying an object. If one person votes against, the object will not be destroyed.
    /// </summary>
    public class VetoVote
    {
        private readonly bool m_Default;
        
        private int m_Cons;
        public bool result { get; private set; }
        private event Action OnResultChange;
        
        public VetoVote(bool @default)
        {
            m_Default = @default;
            result = @default;
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
            action(result);
            
            OnResultChange += ChangeHandler;
            
            return new DisposableAction(() =>
            {
                OnResultChange -= ChangeHandler;
            });

            void ChangeHandler()
            {
                action(result);
            }
        }

        public DisposableAction WatchAsync(Func<bool, Signal, UniTaskVoid> action)
        {
            var css = SignalSource.Create();
            action(result, css.Signal).Forget();
            
            OnResultChange += ChangeHandler;
            
            return new DisposableAction(() =>
            {
                css.TryRaise();
                OnResultChange -= ChangeHandler;
            });

            void ChangeHandler()
            {
                css.TryRaise();
                css = SignalSource.Create();
                action(result, css.Signal).Forget();
            }
        }
        
        private void SummingUp()
        {
            var nextResult = m_Cons > 0 ? !m_Default : m_Default;
            var resultChanged = result != nextResult;
            result = nextResult;
            if (resultChanged)
            {
                OnResultChange?.Invoke();
            }
        }
    }
}