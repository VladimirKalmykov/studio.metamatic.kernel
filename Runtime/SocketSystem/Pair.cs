using System;

namespace Metamatic
{
    public class Pair<TFirst, TSecond>: IPair
    {
        public Type GetFirstType()
        {
            return typeof(TFirst);
        }

        public Type GetSecondType()
        {
            return typeof(TSecond);
        }
    }
}