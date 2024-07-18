using NUnit.Framework;
using System;
using Metamatic;

namespace Tests
{
    public class FinanceUtilsTests
    {
        [Test]
        public void Test_GetExchange_InLargeBills()
        {
            int totalAmount = 27;
            int[] availableDenominations = { 1, 5, 10, 20 };
            int[] expected = { 20, 5, 1, 1 };
            int[] result = FinanceUtils.GetExchange(totalAmount, availableDenominations, FinanceUtils.ExchangeType.InLargeBills, FinanceUtils.RemainderHandling.ExchangeInSmallBills);
            
            CollectionAssert.AreEqual(expected, result);
        }

        [Test]
        public void Test_GetExchange_Optimal()
        {
            int totalAmount = 30;
            int[] availableDenominations = { 1, 10, 25 };
            int[] expected = { 10, 10, 10 };
            int[] result = FinanceUtils.GetExchange(totalAmount, availableDenominations, FinanceUtils.ExchangeType.Optimal, FinanceUtils.RemainderHandling.ExchangeInSmallBills);
            
            CollectionAssert.AreEqual(expected, result);
        }

        [Test]
        public void Test_GetExchange_Present()
        {
            int totalAmount = 37;
            int[] availableDenominations = { 1, 5, 10, 20 };
            int[] expected = { 20, 10, 5, 1, 1 };
            int[] result = FinanceUtils.GetExchange(totalAmount, availableDenominations, FinanceUtils.ExchangeType.Present, FinanceUtils.RemainderHandling.ExchangeInSmallBills);
            
            CollectionAssert.AreEqual(expected, result);
        }

        [Test]
        public void Test_GetPresentExchange_WithMinMaxCoins()
        {
            int totalAmount = 78;
            int[] availableDenominations = { 1, 5, 10, 100 };
            int[] expected = { 10, 10, 10, 10, 10, 10, 10, 5, 1, 1, 1 }; // Example result based on provided logic
            int[] result = FinanceUtils.GetPresentExchange(totalAmount, availableDenominations, FinanceUtils.RemainderHandling.ExchangeInSmallBills, 10, 20);
            
            CollectionAssert.AreEqual(expected, result);
        }

        [Test]
        public void Test_RemainderHandling_ThrowException()
        {
            int totalAmount = 23;
            int[] availableDenominations = { 5, 10 };
            Assert.Throws<InvalidOperationException>(() => FinanceUtils.GetExchange(totalAmount, availableDenominations, FinanceUtils.ExchangeType.InLargeBills, FinanceUtils.RemainderHandling.ThrowException));
        }

        [Test]
        public void Test_RemainderHandling_ExchangeInLargeBills()
        {
            int totalAmount = 23;
            int[] availableDenominations = { 5, 10 };
            int[] expected = { 10, 10, 1, 1, 1 };
            int[] result = FinanceUtils.GetExchange(totalAmount, availableDenominations, FinanceUtils.ExchangeType.InLargeBills, FinanceUtils.RemainderHandling.ExchangeInSmallBills);
            
            CollectionAssert.AreEqual(expected, result);
        }
    }
}
