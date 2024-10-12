using System;
using Cysharp.Threading.Tasks;

namespace Metamatic
{
    /// <summary>
    /// Helper struct to use with PlayerLoopHelper.AddAction method of UniTask library.
    /// </summary>
    /// <example>
    /// PlayerLoopHelper.AddAction(PlayerLoopTiming.Update, new LoopItem(() => { return true; }));
    /// </example>
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