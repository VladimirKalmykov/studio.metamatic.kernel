using System;

namespace Metamatic
{
    public class UniqueDice: IDisposable
    {
        private int m_Sides;
        private int usedSidesMask;
        public int remainingSides { get; private set; }

        public UniqueDice(int sides)
        {
            if (sides > 32)
            {
                throw new ArgumentOutOfRangeException(nameof(sides), "Maximum number of sides is 32.");
            }
            this.m_Sides = sides;
            this.usedSidesMask = 0;
            this.remainingSides = sides;
        }

        public void SetSides(int sides)
        {
            if (sides > 32)
            {
                throw new ArgumentOutOfRangeException(nameof(sides), "Maximum number of sides is 32.");
            }
            this.m_Sides = sides;
            Reset();
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
                side = UnityEngine.Random.Range(0, m_Sides);
            } while ((usedSidesMask & (1 << side)) != 0);

            usedSidesMask |= (1 << side);
            remainingSides--;

            return side;
        }

        public int Roll()
        {
            return Next();
        }
        
        public void Reset()
        {
            usedSidesMask = 0;
            remainingSides = m_Sides;
        }

        public void Dispose()
        {
            Reset();
        }
    }
}