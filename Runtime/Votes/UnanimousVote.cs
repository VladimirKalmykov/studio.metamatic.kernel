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
        public enum Resolution
        {
            Default,
            True,
            False
        }
        private readonly bool m_FallbackResult;
        private int m_TrueVotes;
        private int m_FalseVotes;
        private bool m_PrevResult;
        private Resolution m_PrevResolution = Resolution.Default;
        public bool result { get; private set; }
        public Resolution resolution { get; private set; }
        public event Action<bool, Resolution> OnResultChanged;
        
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
        public DisposableAction Watch(Action<bool, Resolution> action)
        {
            OnResultChanged += action;
            action(result, resolution);
            return new DisposableAction(() => { OnResultChanged -= action; });
        }
        private void SummingUp()
        {
            if (m_TrueVotes > 0 && m_FalseVotes == 0)
            {
                resolution = Resolution.True;
                result = true;
            }
            else if (m_FalseVotes > 0 && m_TrueVotes == 0)
            {
                resolution = Resolution.False;
                result = false;
            }
            else
            {
                resolution = Resolution.Default;
                result = m_FallbackResult;
            }
            if (m_PrevResult != result || m_PrevResolution != resolution)
            {
                m_PrevResult = result;
                m_PrevResolution = resolution;
                OnResultChanged?.Invoke(result, resolution);
            }
        }
    }
}