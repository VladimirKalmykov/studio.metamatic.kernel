using NUnit.Framework;
using UnityEngine;
using Metamatic;

internal class UTestScriptableObject: ScriptableObject {

}

public class VirtualPinTests : MonoBehaviour
{
    [Test]
    public void ConnectPins_CorrectlyEstablishesConnections()
    {
        var testGameObject = new GameObject("Tester");
        var so = ScriptableObject.CreateInstance<UTestScriptableObject>();
        var pin1 = testGameObject.AddComponent<FloatPin>();
        var pin2 = new VirtualPin<float>(so);
        var pin3 = testGameObject.AddComponent<FloatPin>();

        // Создаем связи
        pin1.ConnectOutput(pin2);
        pin2.ConnectOutput(pin3);

        // Проверяем, что связи установлены корректно
        Assert.AreEqual(pin2, pin1.outputPins[0]);
        Assert.AreEqual(pin3, pin2.outputPins[0]);
    }
}