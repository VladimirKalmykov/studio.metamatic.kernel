using System;

namespace Metamatic
{
    public interface ICompetitive
    {
        public int priority { get; }
        public Action SubscribeDispose(Action handler);
    }
}