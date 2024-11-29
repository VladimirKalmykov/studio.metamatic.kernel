using NUnit.Framework;
using UnityEngine;
using Metamatic;

public class AcceleratingPulseValueTests
{
    [Test]
    public void GetValue_ReturnsExpectedValues()
    {
        // Arrange
        float startValue = 0f;
        float initialVelocity = 0f;
        float acceleration = 2f; // units per second squared

        float currentTime = 0f;
        var timeSource = new TimeSource(() => currentTime);

        var pulseValue = new AcceleratingPulseValue(startValue, acceleration, initialVelocity, timeSource);

        // Act & Assert at t = 0
        float valueAt0Seconds = pulseValue.GetValue();
        float expectedValueAt0Seconds = startValue;
        Assert.AreEqual(expectedValueAt0Seconds, valueAt0Seconds, 0.0001f);

        // Simulate time passing
        currentTime = 1f;
        float valueAt1Second = pulseValue.GetValue();
        float expectedValueAt1Second = startValue + 0.5f * acceleration * 1f * 1f;
        Assert.AreEqual(expectedValueAt1Second, valueAt1Second, 0.0001f);

        // Simulate more time passing
        currentTime = 2f;
        float valueAt2Seconds = pulseValue.GetValue();
        float expectedValueAt2Seconds = startValue + 0.5f * acceleration * 2f * 2f;
        Assert.AreEqual(expectedValueAt2Seconds, valueAt2Seconds, 0.0001f);
    }
}