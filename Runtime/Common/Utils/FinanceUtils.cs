using System;
using System.Collections.Generic;
using System.Linq;

namespace Metamatic
{
    public static class FinanceUtils
    {
        public enum ExchangeType
        {
            InLargeBills,
            Optimal,
            Present
        }

        public enum RemainderHandling
        {
            ThrowException,
            ExchangeInSmallBills
        }
        
        public static int[] GetExchange(int totalAmount, int[] availableDenominations, ExchangeType exchangeType,
            RemainderHandling remainderHandling = RemainderHandling.ExchangeInSmallBills)
        {
            switch (exchangeType)
            {
                case ExchangeType.InLargeBills:
                    return GetExchangeInLargeBills(totalAmount, availableDenominations, remainderHandling);
                case ExchangeType.Optimal:
                    return GetOptimalExchange(totalAmount, availableDenominations, remainderHandling);
                case ExchangeType.Present:
                    return GetPresentExchange(totalAmount, availableDenominations, remainderHandling);
                default:
                    throw new ArgumentOutOfRangeException(nameof(exchangeType), exchangeType, null);
            }
        }

        private static int[] GetExchangeInLargeBills(int totalAmount, int[] availableDenominations, RemainderHandling remainderHandling)
        {
            Array.Sort(availableDenominations);
            Array.Reverse(availableDenominations);
            List<int> result = new List<int>();
            int remainingAmount = totalAmount;

            foreach (var denomination in availableDenominations)
            {
                while (remainingAmount >= denomination)
                {
                    result.Add(denomination);
                    remainingAmount -= denomination;
                }
            }

            HandleRemainder(result, remainingAmount, remainderHandling);

            return result.ToArray();
        }

        private static int[] GetOptimalExchange(int totalAmount, int[] availableDenominations,
            RemainderHandling remainderHandling = RemainderHandling.ThrowException)
        {
            int[] dp = new int[totalAmount + 1];
            int[] lastCoin = new int[totalAmount + 1];

            Array.Fill(dp, int.MaxValue);
            dp[0] = 0;

            for (int i = 1; i <= totalAmount; i++)
            {
                foreach (var denomination in availableDenominations)
                {
                    if (i >= denomination && dp[i - denomination] != int.MaxValue && dp[i - denomination] + 1 < dp[i])
                    {
                        dp[i] = dp[i - denomination] + 1;
                        lastCoin[i] = denomination;
                    }
                }
            }

            List<int> result = new List<int>();
            int remainingAmount = totalAmount;

            while (remainingAmount > 0)
            {
                if (lastCoin[remainingAmount] == 0)
                {
                    HandleRemainder(result, remainingAmount, remainderHandling);
                    break;
                }
                else
                {
                    result.Add(lastCoin[remainingAmount]);
                    remainingAmount -= lastCoin[remainingAmount];
                }
            }

            return result.ToArray();
        }

        public static int[] GetPresentExchange(int totalAmount, int[] availableDenominations,
            RemainderHandling remainderHandling, int minCoins = 5, int maxCoins = 50)
        {
            List<int> result = new List<int>();
            int remainingAmount = totalAmount;
            Array.Sort(availableDenominations);

            maxCoins = Math.Max(minCoins, Math.Min(maxCoins, totalAmount / availableDenominations.Min())); // Гарантируем, что maxCoins >= minCoins и не больше totalAmount / минимальная купюра

            foreach (var denomination in availableDenominations.Reverse())
            {
                while (remainingAmount >= denomination && result.Count < maxCoins)
                {
                    result.Add(denomination);
                    remainingAmount -= denomination;
                }
            }

            if (result.Count < maxCoins && remainingAmount > 0)
            {
                int additionalCoins = Math.Min(maxCoins - result.Count, remainingAmount);
                result.AddRange(Enumerable.Repeat(1, additionalCoins));
                remainingAmount -= additionalCoins;
            }

            HandleRemainder(result, remainingAmount, remainderHandling);

            return result.ToArray();
        }

        private static void HandleRemainder(List<int> result, int remainingAmount, RemainderHandling remainderHandling)
        {
            switch (remainderHandling)
            {
                case RemainderHandling.ExchangeInSmallBills:
                    while (remainingAmount > 0)
                    {
                        result.Add(1);
                        remainingAmount -= 1;
                    }
                    break;
                case RemainderHandling.ThrowException:
                    if (remainingAmount > 0)
                    {
                        throw new InvalidOperationException(
                            $"Невозможно полностью разменять сумму. Остаток: {remainingAmount}");
                    }
                    break;
            }
        }
    }
}
