using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Metamatic;

public class PinTests : MonoBehaviour
{
    [Test]
    public void ConnectPins_CorrectlyEstablishesConnections()
    {
        var testGameObject = new GameObject("Tester");
        var pin1 = testGameObject.AddComponent<FloatPin>();
        var pin2 = testGameObject.AddComponent<FloatPin>();
        var pin3 = testGameObject.AddComponent<FloatPin>();

        // Создаем связи
        pin1.ConnectOutput(pin2);
        pin2.ConnectOutput(pin3);

        // Проверяем, что связи установлены корректно
        Assert.AreEqual(pin2, pin1.outputPins[0]);
        Assert.AreEqual(pin3, pin2.outputPins[0]);
    }

    [Test]
    public void WireData_ShouldBeConsistentAcrossConnectedPins()
    {
        var pin1 = new GameObject().AddComponent<FloatPin>();
        var pin2 = new GameObject().AddComponent<FloatPin>();
        var pin3 = new GameObject().AddComponent<FloatPin>();
        
        // Создаем связи
        pin1.ConnectOutput(pin2);
        pin2.ConnectOutput(pin3);
        
        // Проверяем, что данные в связях совпадают
        Assert.AreEqual(pin1.wire.data, pin2.wire.data);
        Assert.AreEqual(pin2.wire.data, pin3.wire.data);
        
        // Проверяем изменения данных
        pin1.wire.SetData(1);
        Assert.AreEqual(pin1.wire.data, pin2.wire.data);
        Assert.AreEqual(pin2.wire.data, pin3.wire.data);
        
        // Проверяем что не получится изменить данные у подключенного пина
        pin2.wire.SetData(2);
        Assert.AreEqual(pin1.wire.data, pin2.wire.data);
    }
    
    [Test]
    public void DisconnectInput_ShouldRemoveConnection()
    {
        var pin1 = new GameObject().AddComponent<FloatPin>();
        var pin2 = new GameObject().AddComponent<FloatPin>();
        var pin3 = new GameObject().AddComponent<FloatPin>();
        
        // Создаем связи
        pin1.ConnectOutput(pin2);
        pin2.ConnectOutput(pin3);
        
        pin1.wire.SetData(1);
        // Проверяем, что данные в связях совпадают
        Assert.AreEqual(pin1.wire.data, pin2.wire.data);
        Assert.AreEqual(pin2.wire.data, pin3.wire.data);
        
        // Отключаем связь
        pin2.DisconnectInput();
        
        // Проверяем, что данные в связях не совпадают
        Assert.AreNotEqual(pin1.wire.data, pin2.wire.data);
        Assert.AreEqual(pin2.wire.data, pin3.wire.data);
    }

    [Test]
    public void DestroySourcePinOnTheFly()
    {
        var pin1 = new GameObject().AddComponent<FloatPin>();
        var pin2 = new GameObject().AddComponent<FloatPin>();
        var pin3 = new GameObject().AddComponent<FloatPin>();
        
        /* Make connections */
        pin1.ConnectOutput(pin2);
        pin2.ConnectOutput(pin3);
        
        /* Set data */
        pin1.wire.SetData(1);
        
        /* Check data */
        Assert.AreEqual(pin2.wire.data, 1);
        Assert.AreEqual(pin3.wire.data, 1);
        
        /* Destroy source pin */
        Object.DestroyImmediate(pin1.gameObject);
        
        /* Check data */
        Assert.AreEqual(pin2.wire.data, 0);
        Assert.AreEqual(pin3.wire.data, 0);
    }
    
    [Test]
    public void ConnectPinToItself()
    {
        var pin1 = new GameObject().AddComponent<FloatPin>();
        
        /* Make connections between the same pin should throw errror */
        Assert.Throws<System.InvalidOperationException>(() => pin1.ConnectOutput(pin1));
    }

    [Test]
    public void ConnectTwoPinsToOnePin()
    {
        var pin1 = new GameObject().AddComponent<FloatPin>();
        var pin2 = new GameObject().AddComponent<FloatPin>();
        var pin3 = new GameObject().AddComponent<FloatPin>();
        
        /* Make connections */
        pin2.ConnectInput(pin1);
        pin3.ConnectInput(pin1);
        
        /* Set data */
        pin1.wire.SetData(1);
        
        /* Check data */
        Assert.AreEqual(pin2.wire.data, 1);
        Assert.AreEqual(pin3.wire.data, 1);
    }

    [Test]
    public void StillConnectedOnDisable()
    {
        var pin1 = new GameObject().AddComponent<FloatPin>();
        var pin2 = new GameObject().AddComponent<FloatPin>();
        var pin3 = new GameObject().AddComponent<FloatPin>();
        
        /* Make connections */
        pin1.ConnectOutput(pin2);
        pin2.ConnectOutput(pin3);
        
        /* Set data */
        pin1.wire.SetData(1);
        
        /* Check that pin1 has output pin and pin 3 has input pin */
        Assert.IsTrue(pin1.hasOutputPins);
        Assert.IsTrue(pin3.hasInputPin);
        
        /* Disable pin2 */
        pin2.enabled = false;
        
        /* Check that pin1 has output pin and pin 3 has input pin */
        Assert.IsTrue(pin1.hasOutputPins);
        Assert.IsTrue(pin3.hasInputPin);
    }
    
    [Test]
    public void AutoDisconnectedOnDestroy()
    {
        var pin1 = new GameObject().AddComponent<FloatPin>();
        var pin2 = new GameObject().AddComponent<FloatPin>();
        var pin3 = new GameObject().AddComponent<FloatPin>();
        
        /* Make connections */
        pin1.ConnectOutput(pin2);
        pin2.ConnectOutput(pin3);
        
        /* Set data */
        pin1.wire.SetData(1);
        
        /* Check that pin1 has output pin and pin 3 has input pin */
        Assert.IsTrue(pin1.hasOutputPins);
        Assert.IsTrue(pin3.hasInputPin);
        
        /* Check that wires have connected too */
        Assert.IsTrue(pin1.wire.outputWires.Contains(pin2.wire));
        Assert.IsTrue(pin3.wire.inputWire == pin2.wire);
        
        /* Destroy pin2 */
        Object.DestroyImmediate(pin2.gameObject);
        
        /* Check that pin1 has no output pin and pin 3 has no input pin */
        Assert.IsFalse(pin1.hasOutputPins);
        Assert.IsFalse(pin3.hasInputPin);
        
        /* Check that wires have disconnected too */
        Assert.IsFalse(pin1.wire.outputWires.Contains(pin2.wire));
        Assert.IsFalse(pin3.wire.inputWire == pin2.wire);
    }
}