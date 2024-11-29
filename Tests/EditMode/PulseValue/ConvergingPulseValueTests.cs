using NUnit.Framework;
using UnityEngine;
using Metamatic;

public class ConvergingPulseValueTests
{
    [Test]
    public void GetValue_ReturnsExpectedValues()
    {
        // Arrange
        float startValue = 0f;
        float endpoint = 10f;
        float rate = 0.5f;

        float currentTime = 0f;
        TimeSource timeProvider = new TimeSource(() => currentTime);

        var pulseValue = new ConvergingPulseValue(startValue, endpoint, rate, timeProvider);

        // Act & Assert at t = 0
        float valueAt0Seconds = pulseValue.GetValue();
        Assert.AreEqual(startValue, valueAt0Seconds, 0.0001f);

        // Simulate time passing
        currentTime = 1f;
        float valueAt1Second = pulseValue.GetValue();
        float expectedValueAt1Second = endpoint - (endpoint - startValue) * Mathf.Exp(-rate * 1f * 1f);
        Assert.AreEqual(expectedValueAt1Second, valueAt1Second, 0.0001f);

        // Simulate more time passing
        currentTime = 2f;
        float valueAt2Seconds = pulseValue.GetValue();
        float expectedValueAt2Seconds = endpoint - (endpoint - startValue) * Mathf.Exp(-rate * 4f);
        Assert.AreEqual(expectedValueAt2Seconds, valueAt2Seconds, 0.0001f);
    }
}