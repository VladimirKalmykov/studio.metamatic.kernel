
using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Metamatic.Tests
{

[TestFixture]
public class AsyncBridgeTests
{
    [UnityTest]
    public IEnumerator EmptyTest() => UniTask.ToCoroutine(async () =>
    {
        var a = UniTask.FromResult(true);
        var result = await a;
        Assert.IsTrue(result);
    });
    
    [UnityTest]
    public IEnumerator TestSimpleReceive() => UniTask.ToCoroutine(async () =>
    {
        var state = 0;
        await using var bridge = AsyncBridge<int, int>.Create(async (exchanger, token) =>
        {
            var receivedValue = await exchanger.Receive();
            state = receivedValue;
        });
        Assert.AreEqual(0, state);
        bridge.Send(1);
        Assert.AreEqual(1, state);
    });

    [UnityTest]
    public IEnumerator TestSimpleSend() => UniTask.ToCoroutine(async () =>
    {
        var state = 0;
        await using var bridge = AsyncBridge<int, int>.Create((exchanger, token) =>
        {
            exchanger.Send(1);
            return UniTask.CompletedTask;
        });
        Assert.AreEqual(0, state);
        state = await bridge.Receive();
        Assert.AreEqual(1, state);
    });

    [UnityTest]
    public IEnumerator TestSimpleDuplex() => UniTask.ToCoroutine(async () =>
    {
        var innerState = 0;
        var outerState = 0;
        await using var bridge = AsyncBridge<int, int>.Create(async (exchanger, token) =>
        {
            innerState = await exchanger.Duplex(33);
        });
        Assert.AreEqual(0, innerState);
        Assert.AreEqual(0, outerState);
        outerState = await bridge.Duplex(44);
        Assert.AreEqual(44, innerState);
        Assert.AreEqual(33, outerState);
    });
}

}