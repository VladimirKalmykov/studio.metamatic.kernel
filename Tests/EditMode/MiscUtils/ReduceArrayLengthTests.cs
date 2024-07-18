using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Metamatic
{
    [TestFixture]
    public class ReduceArrayLengthTests
    {
        [Test]
        public void ReduceArray_InputLengthLessThanOrEqualToMaxLength_ReturnsOriginalArray()
        {
            // Arrange
            List<int> inputList = new List<int> { 1, 2, 3 };
            int maxLength = 5;

            // Act
            int[] result = MiscUtils.ReduceArrayLength(inputList, maxLength);

            // Assert
            Assert.AreEqual(inputList.ToArray(), result);
        }

        [Test]
        public void ReduceArray_InputLengthGreaterThanMaxLength_ReturnsReducedArray()
        {
            // Arrange
            List<int> inputList = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            int maxLength = 5;

            // Act
            int[] result = MiscUtils.ReduceArrayLength(inputList, maxLength);

            // Assert
            int[] expected = new int[] { 1, 4, 7, 9, 12 };
            Debug.Log($"Result: {result}");
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void ReduceArray_InputLengthEqualsMaxLength_ReturnsOriginalArray()
        {
            // Arrange
            List<int> inputList = new List<int> { 1, 2, 3, 4, 5 };
            int maxLength = 5;

            // Act
            int[] result = MiscUtils.ReduceArrayLength(inputList, maxLength);

            // Assert
            Assert.AreEqual(inputList.ToArray(), result);
        }

        [Test]
        public void ReduceArray_InputLengthMuchGreaterThanMaxLength_ReturnsReducedArray()
        {
            // Arrange
            List<int> inputList = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
            int maxLength = 5;

            // Act
            int[] result = MiscUtils.ReduceArrayLength(inputList, maxLength);

            // Assert
            int[] expected = new int[] { 1, 6, 11, 15, 20 };
            Assert.AreEqual(expected, result);
        }
        
    }
}