using System.Collections.Generic;
using System.Linq;

namespace Metamatic
{
    public static class Bitmask
    {
        public static int Create(IEnumerable<int> numbers)
        {
            return numbers.Aggregate(0, (current, n) => current | (1 << n));
        }

        public static int Create(int n)
        {
            return Create(new[] {n});
        }

        public static int Append(int bitmask, IEnumerable<int> numbers)
        {
            return numbers.Aggregate(bitmask, (current, n) => current | (1 << n));
        }

        public static int Append(int bitmask, int n)
        {
            return Append(bitmask, new[] {n});
        }

        public static int Remove(int bitmask, IEnumerable<int> numbers)
        {
            return numbers.Aggregate(bitmask, (current, n) => current ^ (1 << n));
        }

        public static int Remove(int bitmask, int n)
        {
            return Remove(bitmask, new[] {n});
        }

        public static bool Test(int bitmask, IEnumerable<int> numbers)
        {
            return numbers.All(n => (bitmask & (1 << n)) != 0);
        }

        public static bool Test(int bitmask, int n)
        {
            return Test(bitmask, new[] {n});
        }
        
        /// <summary>
        /// Get set bits indices 
        /// </summary>
        /// <example>
        /// For example, if you have a bitmask 00001010, this function will return a list of indices [1, 3]
        /// </example>
        /// <param name="mask"></param>
        /// <returns></returns>
        public static int[] ExtractSetBitIndices(ulong mask)
        {
            List<int> indices = new List<int>();

            for (int i = 0; i < 64; i++)
            {
                if ((mask & (1UL << i)) != 0)
                {
                    indices.Add(i);
                }
            }

            return indices.ToArray();
        }
    }
}