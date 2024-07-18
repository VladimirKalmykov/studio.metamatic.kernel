namespace Metamatic.EditModeTests
{
    using NUnit.Framework;
using UnityEngine;
using Metamatic;

public class VirtualPinTests : MonoBehaviour
{
    [Test]
    public void ConnectPins_CorrectlyEstablishesConnections()
    {
        var pin1 = new VirtualPin<float>(this);
        var pin2 = new VirtualPin<float>(this);
        var pin3 = new VirtualPin<float>(this);

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
        var pin1 = new VirtualPin<float>(this);
        var pin2 = new VirtualPin<float>(this);
        var pin3 = new VirtualPin<float>(this);
        
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
        var pin1 = new VirtualPin<float>(this);
        var pin2 = new VirtualPin<float>(this);
        var pin3 = new VirtualPin<float>(this);
        
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
        var pin1 = new VirtualPin<float>(this);
        var pin2 = new VirtualPin<float>(this);
        var pin3 = new VirtualPin<float>(this);
        
        /* Make connections */
        pin1.ConnectOutput(pin2);
        pin2.ConnectOutput(pin3);
        
        /* Set data */
        pin1.wire.SetData(1);
        
        /* Check data */
        Assert.AreEqual(pin2.wire.data, 1);
        Assert.AreEqual(pin3.wire.data, 1);
        
        /* Destroy source pin */
        pin1.Dispose();
        
        /* Check data */
        Assert.AreEqual(pin2.wire.data, 0);
        Assert.AreEqual(pin3.wire.data, 0);
    }
    
    [Test]
    public void ConnectPinToItself()
    {
        var pin1 = new VirtualPin<float>(this);
        
        /* Make connections between the same pin should throw errror */
        Assert.Throws<System.InvalidOperationException>(() => pin1.ConnectOutput(pin1));
    }

    [Test]
    public void ConnectTwoPinsToOnePin()
    {
        var pin1 = new VirtualPin<float>(this);
        var pin2 = new VirtualPin<float>(this);
        var pin3 = new VirtualPin<float>(this);
        
        /* Make connections */
        pin2.ConnectInput(pin1);
        pin3.ConnectInput(pin1);
        
        /* Set data */
        pin1.wire.SetData(1);
        
        /* Check data */
        Assert.AreEqual(pin2.wire.data, 1);
        Assert.AreEqual(pin3.wire.data, 1);
    }
}
}