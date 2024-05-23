using System;

namespace Metamatic
{
    public class UniqueDice
    {
        private readonly int sides;
        private int usedSidesMask;
        private int remainingSides;

        public UniqueDice(int sides)
        {
            if (sides > 32)
            {
                throw new ArgumentOutOfRangeException(nameof(sides), "Maximum number of sides is 32.");
            }
            this.sides = sides;
            this.usedSidesMask = 0;
            this.remainingSides = sides;
        }

        public int Next()
        {
            if (remainingSides == 0)
            {
                return -1; // No more sides left
            }

            int side;
            do
            {
                side = UnityEngine.Random.Range(0, sides);
            } while ((usedSidesMask & (1 << side)) != 0);

            usedSidesMask |= (1 << side);
            remainingSides--;

            return side;
        }
        
        public void Reset()
        {
            usedSidesMask = 0;
            remainingSides = sides;
        }
    }
}