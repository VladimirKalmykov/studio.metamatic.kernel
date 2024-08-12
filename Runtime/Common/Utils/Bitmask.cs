using System;
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
        
        public static bool HasFlag(int value, int flag)
        {
            return (value & flag) == flag;
        }
        
        public static bool HasFlag(long value, int flag)
        {
            return (value & flag) == flag;
        }
        
        /// <summary>
        /// Get difference between two versions of bit mask. It returns added and removed flags.
        /// </summary>
        /// <param name="oldFlags"></param>
        /// <param name="newFlags"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static (T added, T removed) GetFlagsDiff<T>(T oldFlags, T newFlags) where T : Enum
        {
            // Преобразуем enum в целочисленные значения
            long oldFlagsValue = Convert.ToInt64(oldFlags);
            long newFlagsValue = Convert.ToInt64(newFlags);
        
            // Находим добавленные флаги
            T added = (T)Enum.ToObject(typeof(T), newFlagsValue & ~oldFlagsValue);
        
            // Находим удаленные флаги
            T removed = (T)Enum.ToObject(typeof(T), oldFlagsValue & ~newFlagsValue);

            return (added, removed);
        }

        public static T[] ExtractArray<T>(T flags) where T : Enum
        {
            // Проверяем, если flags равен 0 (что означает отсутствие установленных флагов)
            if (Convert.ToInt32(flags) == 0)
            {
                return Array.Empty<T>(); // Возвращаем пустой массив
            }

            return Enum.GetValues(typeof(T))
                .Cast<T>()
                .Where(flag => Convert.ToInt32(flag) != 0 && flags.HasFlag(flag))
                .ToArray();
        }
    }
}