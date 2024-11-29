using NUnit.Framework;
using UnityEngine;
using Metamatic;

public class SpringPulseValueTests
{
    [Test]
    public void GetValue_ReturnsExpectedValues()
    {
        // Arrange
        float startValue = 10f;
        float endpoint = 0f;
        float decayRate = 1f;
        float minSpeed = 0.1f;

        float currentTime = 0f;
        TimeSource timeProvider = new TimeSource(() => currentTime);

        var pulseValue = new SpringPulseValue(startValue, endpoint, decayRate, minSpeed, timeProvider);

        // Act & Assert at t = 0
        float valueAt0Seconds = pulseValue.GetValue();
        Assert.AreEqual(startValue, valueAt0Seconds, 0.0001f);

        // Simulate time passing
        currentTime = 1f;
        float valueAt1Second = pulseValue.GetValue();
        float expDecay = Mathf.Exp(-decayRate * 1f);
        float expectedValueAt1Second = endpoint + (startValue - endpoint) * expDecay;
        Assert.AreEqual(expectedValueAt1Second, valueAt1Second, 0.0001f);

        // Simulate more time passing
        currentTime = 5f;
        float valueAt5Seconds = pulseValue.GetValue();
        expDecay = Mathf.Exp(-decayRate * 5f);
        float expectedValueAt5Seconds = endpoint + (startValue - endpoint) * expDecay;

        // Since speed might have hit minSpeed, we need to check accordingly
        float speed = Mathf.Abs(decayRate * (startValue - endpoint) * expDecay);
        if (speed < minSpeed)
        {
            float adjustedExpDecay = minSpeed * 5f / Mathf.Abs(startValue - endpoint);
            expectedValueAt5Seconds = endpoint + (startValue - endpoint) * Mathf.Exp(-adjustedExpDecay);
        }

        Assert.AreEqual(expectedValueAt5Seconds, valueAt5Seconds, 0.0001f);
    }
}