using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    [Obsolete("Use something else instead", true)]
    public class PlayerLoopAction: IPlayerLoopItem
    {
        private readonly Func<bool> m_PlayerLoopAction;
        private readonly CancellationToken m_Token;
        public PlayerLoopAction(Func<bool> action, CancellationToken token)
        {
            m_PlayerLoopAction = action;
        }
        public bool MoveNext()
        {
            if (m_Token.IsCancellationRequested)
            {
                return false;
            }
            return m_PlayerLoopAction();
        }
    }
}