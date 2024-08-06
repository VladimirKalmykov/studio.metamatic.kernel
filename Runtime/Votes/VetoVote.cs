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
            return new DisposableAction(() =>
            {
                if (!value.Equals(m_Default))
                {
                    m_Cons--;
                }
                SummingUp();
            });
        }
        
        private void SummingUp()
        {
            result = m_Cons > 0 ? !m_Default : m_Default;
        }
    }
}