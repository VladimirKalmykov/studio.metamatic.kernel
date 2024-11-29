using NUnit.Framework;
using UnityEngine;
using Metamatic;

public class DeceleratingPulseValueTests
{
    [Test]
    public void GetValue_ReturnsExpectedValues()
    {
        // Arrange
        float startValue = 0f;
        float initialVelocity = 10f;
        float decayRate = 1f;

        float currentTime = 0f;
        TimeSource timeProvider = new TimeSource(() => currentTime);

        var pulseValue = new DeceleratingPulseValue(startValue, initialVelocity, decayRate, timeProvider);

        // Act & Assert at t = 0
        float valueAt0Seconds = pulseValue.GetValue();
        Assert.AreEqual(startValue, valueAt0Seconds, 0.0001f);

        // Simulate time passing
        currentTime = 1f;
        float valueAt1Second = pulseValue.GetValue();
        float expectedValueAt1Second = startValue + (initialVelocity / decayRate) * (1 - Mathf.Exp(-decayRate * 1f));
        Assert.AreEqual(expectedValueAt1Second, valueAt1Second, 0.0001f);

        // Simulate more time passing
        currentTime = 2f;
        float valueAt2Seconds = pulseValue.GetValue();
        float expectedValueAt2Seconds = startValue + (initialVelocity / decayRate) * (1 - Mathf.Exp(-decayRate * 2f));
        Assert.AreEqual(expectedValueAt2Seconds, valueAt2Seconds, 0.0001f);
    }
}