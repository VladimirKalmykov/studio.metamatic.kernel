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
}