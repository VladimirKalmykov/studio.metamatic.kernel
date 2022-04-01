using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    }
}