using System;

namespace Metamatic
{
    /// <summary>
    /// UnanimousVote solves the problem of voting for a boolean value when only unanimous decisions are important.
    /// Result will always be from fallbackResult if the result of the vote is not unanimous.
    /// For example, if fallback result is false and there are 3 votes, 2 true and 1 false, the result will be false.
    /// </summary>
    public class UnanimousVote
    {
        private readonly bool m_FallbackResult;
        private int m_TrueVotes;
        private int m_FalseVotes;
        private bool m_PrevResult;
        public bool result { get; private set; }
        public event Action<bool> OnResultChanged;
        public UnanimousVote(bool fallbackResult)
        {
            m_PrevResult = fallbackResult;
            result = fallbackResult;
            m_FallbackResult = fallbackResult;
        }
        public DisposableAction Vote(bool vote)
        {
            if (vote)
            {
                m_TrueVotes++;
            }
            else
            {
                m_FalseVotes++;
            }
            SummingUp();
            return new DisposableAction(() =>
            {
                if (vote)
                {
                    m_TrueVotes--;
                }
                else
                {
                    m_FalseVotes--;
                }
                SummingUp();
            });
        }
        public DisposableAction Subscribe(Action<bool> action)
        {
            OnResultChanged += action;
            action(result);
            return new DisposableAction(() => { OnResultChanged -= action; });
        }
        private void SummingUp()
        {
            if (m_TrueVotes > 0 && m_FalseVotes == 0)
            {
                result = true;
            }
            else if (m_FalseVotes > 0 && m_TrueVotes == 0)
            {
                result = false;
            }
            else
            {
                result = m_FallbackResult;
            }
            if (m_PrevResult != result)
            {
                m_PrevResult = result;
                OnResultChanged?.Invoke(result);
            }
        }
    }
}