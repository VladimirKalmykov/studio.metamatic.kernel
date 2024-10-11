using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    public readonly struct LoopItem: IPlayerLoopItem
    {
        private readonly Func<bool> m_Action;
        
        public LoopItem(Func<bool> action)
        {
            m_Action = action;
        }
        public bool MoveNext()
        {
            return m_Action();
        }
    }
}