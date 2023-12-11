using System;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using NUnit.Framework;
using Debug = UnityEngine.Debug;

namespace Metamatic
{
    class TestState
    {
        public int intValue;
        public bool bootValue;
        public string stringValue;
        public float floatValue;
        public Vector2 vector2Value;
        public Vector3 vector3Value;
        public Vector4 vector4Value;
        public Quaternion quaternionValue;
        public DateTime dateTimeValue;
        public TimeSpan timeSpanValue;
        public Guid guidValue;
        public byte byteValue;
        public sbyte sbyteValue;
        public short shortValue;
        public ushort ushortValue;
        public uint uintValue;

        public static TestState getRandomValues()
        {
            var newState = new TestState
            {
                intValue = UnityEngine.Random.Range(int.MinValue, int.MaxValue),
                bootValue = UnityEngine.Random.Range(0, 2) == 1,
                stringValue = UnityEngine.Random.Range(0, 2) == 1 ? "Hello" : "World",
                floatValue = UnityEngine.Random.Range(float.MinValue, float.MaxValue),
                vector2Value = new Vector2(UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue)),
                vector3Value = new Vector3(UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue)),
                vector4Value = new Vector4(UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue)),
                quaternionValue = new Quaternion(UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue)),
                dateTimeValue = DateTime.Now,
                timeSpanValue = TimeSpan.FromMilliseconds(UnityEngine.Random.Range(int.MinValue, int.MaxValue)),
                guidValue = Guid.NewGuid(),
                byteValue = (byte)UnityEngine.Random.Range(byte.MinValue, byte.MaxValue),
                sbyteValue = (sbyte)UnityEngine.Random.Range(sbyte.MinValue, sbyte.MaxValue),
                shortValue = (short)UnityEngine.Random.Range(short.MinValue, short.MaxValue),
                ushortValue = (ushort)UnityEngine.Random.Range(ushort.MinValue, ushort.MaxValue),
                uintValue = (uint)UnityEngine.Random.Range(uint.MinValue, uint.MaxValue)
            };
            return newState;
        }
    }
    struct TestStructState
    {
        public int intValue;
        public bool bootValue;
        public string stringValue;
        public float floatValue;
        public Vector2 vector2Value;
        public Vector3 vector3Value;
        public Vector4 vector4Value;
        public Quaternion quaternionValue;
        public DateTime dateTimeValue;
        public TimeSpan timeSpanValue;
        public Guid guidValue;
        public byte byteValue;
        public sbyte sbyteValue;
        public short shortValue;
        public ushort ushortValue;
        public uint uintValue;

        public static TestStructState getRandomValues()
        {
            var newState = new TestStructState
            {
                intValue = UnityEngine.Random.Range(int.MinValue, int.MaxValue),
                bootValue = UnityEngine.Random.Range(0, 2) == 1,
                stringValue = UnityEngine.Random.Range(0, 2) == 1 ? "Hello" : "World",
                floatValue = UnityEngine.Random.Range(float.MinValue, float.MaxValue),
                vector2Value = new Vector2(UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue)),
                vector3Value = new Vector3(UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue)),
                vector4Value = new Vector4(UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue)),
                quaternionValue = new Quaternion(UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue)),
                dateTimeValue = DateTime.Now,
                timeSpanValue = TimeSpan.FromMilliseconds(UnityEngine.Random.Range(int.MinValue, int.MaxValue)),
                guidValue = Guid.NewGuid(),
                byteValue = (byte)UnityEngine.Random.Range(byte.MinValue, byte.MaxValue),
                sbyteValue = (sbyte)UnityEngine.Random.Range(sbyte.MinValue, sbyte.MaxValue),
                shortValue = (short)UnityEngine.Random.Range(short.MinValue, short.MaxValue),
                ushortValue = (ushort)UnityEngine.Random.Range(ushort.MinValue, ushort.MaxValue),
                uintValue = (uint)UnityEngine.Random.Range(uint.MinValue, uint.MaxValue)
            };
            return newState;
        }
    }
    struct HugeTestStructState
    {
        public int intValue;
        public bool bootValue;
        public string stringValue;
        public float floatValue;
        public Vector2 vector2Value;
        public Vector3 vector3Value;
        public Vector4 vector4Value;
        public Quaternion quaternionValue;
        public DateTime dateTimeValue;
        public TimeSpan timeSpanValue;
        public Guid guidValue;
        public byte byteValue;
        public sbyte sbyteValue;
        public short shortValue;
        public ushort ushortValue;
        public uint uintValue;
        public int intValue2;
        public bool bootValue2;
        public string stringValue2;
        public float floatValue2;
        public Vector2 vector2Value2;
        public Vector3 vector3Value2;
        public Vector4 vector4Value2;
        public Quaternion quaternionValue2;
        public DateTime dateTimeValue2;
        public TimeSpan timeSpanValue2;
        public Guid guidValue2;
        public byte byteValue2;
        public sbyte sbyteValue2;
        public short shortValue2;
        public ushort ushortValue2;
        public uint uintValue2;
        public int intValue3;
        public bool bootValue3;
        public string stringValue3;
        public float floatValue3;
        public Vector2 vector2Value3;
        public Vector3 vector3Value3;
        public Vector4 vector4Value3;
        public Quaternion quaternionValue3;
        public DateTime dateTimeValue3;
        public TimeSpan timeSpanValue3;
        public Guid guidValue3;
        public byte byteValue3;
        public sbyte sbyteValue3;
        public short shortValue3;
        public ushort ushortValue3;
        public uint uintValue3;

        public static HugeTestStructState getRandomValues()
        {
            var newState = new HugeTestStructState
            {
                intValue = UnityEngine.Random.Range(int.MinValue, int.MaxValue),
                bootValue = UnityEngine.Random.Range(0, 2) == 1,
                stringValue = UnityEngine.Random.Range(0, 2) == 1 ? "Hello" : "World",
                floatValue = UnityEngine.Random.Range(float.MinValue, float.MaxValue),
                vector2Value = new Vector2(UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue)),
                vector3Value = new Vector3(UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue)),
                vector4Value = new Vector4(UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue)),
                quaternionValue = new Quaternion(UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue)),
                dateTimeValue = DateTime.Now,
                timeSpanValue = TimeSpan.FromMilliseconds(UnityEngine.Random.Range(int.MinValue, int.MaxValue)),
                guidValue = Guid.NewGuid(),
                byteValue = (byte)UnityEngine.Random.Range(byte.MinValue, byte.MaxValue),
                sbyteValue = (sbyte)UnityEngine.Random.Range(sbyte.MinValue, sbyte.MaxValue),
                shortValue = (short)UnityEngine.Random.Range(short.MinValue, short.MaxValue),
                ushortValue = (ushort)UnityEngine.Random.Range(ushort.MinValue, ushort.MaxValue),
                uintValue = (uint)UnityEngine.Random.Range(uint.MinValue, uint.MaxValue),
                intValue2 = UnityEngine.Random.Range(int.MinValue, int.MaxValue),
                bootValue2 = UnityEngine.Random.Range(0, 2) == 1,
                stringValue2 = UnityEngine.Random.Range(0, 2) == 1 ? "Hello" : "World",
                floatValue2 = UnityEngine.Random.Range(float.MinValue, float.MaxValue),
                vector2Value2 = new Vector2(UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue)),
                vector3Value2 = new Vector3(UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue)),
                vector4Value2 = new Vector4(UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue)),
                quaternionValue2 = new Quaternion(UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue)),
                dateTimeValue2 = DateTime.Now,
                timeSpanValue2 = TimeSpan.FromMilliseconds(UnityEngine.Random.Range(int.MinValue, int.MaxValue)),
                guidValue2 = Guid.NewGuid(),
                byteValue2 = (byte)UnityEngine.Random.Range(byte.MinValue, byte.MaxValue),
                sbyteValue2 = (sbyte)UnityEngine.Random.Range(sbyte.MinValue, sbyte.MaxValue),
                shortValue2 = (short)UnityEngine.Random.Range(short.MinValue, short.MaxValue),
                ushortValue2 = (ushort)UnityEngine.Random.Range(ushort.MinValue, ushort.MaxValue),
                uintValue2 = (uint)UnityEngine.Random.Range(uint.MinValue, uint.MaxValue),
                intValue3 = UnityEngine.Random.Range(int.MinValue, int.MaxValue),
                bootValue3 = UnityEngine.Random.Range(0, 2) == 1,
                stringValue3 = UnityEngine.Random.Range(0, 2) == 1 ? "Hello" : "World",
                floatValue3 = UnityEngine.Random.Range(float.MinValue, float.MaxValue),
                vector2Value3 = new Vector2(UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue)),
                vector3Value3 = new Vector3(UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue)),
                vector4Value3 = new Vector4(UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue)),
                quaternionValue3 = new Quaternion(UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue), UnityEngine.Random.Range(float.MinValue, float.MaxValue)),
                dateTimeValue3 = DateTime.Now,
                timeSpanValue3 = TimeSpan.FromMilliseconds(UnityEngine.Random.Range(int.MinValue, int.MaxValue)),
                guidValue3 = Guid.NewGuid(),
                byteValue3 = (byte)UnityEngine.Random.Range(byte.MinValue, byte.MaxValue),
                sbyteValue3 = (sbyte)UnityEngine.Random.Range(sbyte.MinValue, sbyte.MaxValue),
                shortValue3 = (short)UnityEngine.Random.Range(short.MinValue, short.MaxValue),
                ushortValue3 = (ushort)UnityEngine.Random.Range(ushort.MinValue, ushort.MaxValue),
                uintValue3 = (uint)UnityEngine.Random.Range(uint.MinValue, uint.MaxValue)
            };
            return newState;
        }
    }
    public class WireTests
    {
        [Test]
        public void SingleWire()
        {
            var wire = new Wire<int>(0);
            var vfs = 0;
            wire.Subscribe((ref int value) =>
            {
                vfs = value;
            });
            wire.SetData(1);
            Assert.AreEqual(1, vfs);
            Assert.AreEqual(1, wire.data);
            wire.SetData((ref int currentValue) => currentValue + 2);
            Assert.AreEqual(3, vfs);
            Assert.AreEqual(3, wire.data);
        }

        [Test]
        public void TwoWires()
        {
            var wire1 = new Wire<int>(15);
            var wire2 = new Wire<int>();
            wire1.ConnectOutput(wire2);
            Assert.AreEqual(15, wire2.data);
        }
        
        [Test]
        public void TenWires()
        {
            var wire1 = new Wire<int>(15);
            var wire2 = new Wire<int>();
            var wire3 = new Wire<int>();
            var wire4 = new Wire<int>();
            var wire5 = new Wire<int>();
            var wire6 = new Wire<int>();
            var wire7 = new Wire<int>();
            var wire8 = new Wire<int>();
            var wire9 = new Wire<int>();
            var wire10 = new Wire<int>();
            wire1.ConnectOutput(wire2);
            wire2.ConnectOutput(wire3);
            wire3.ConnectOutput(wire4);
            wire4.ConnectOutput(wire5);
            wire5.ConnectOutput(wire6);
            wire6.ConnectOutput(wire7);
            wire7.ConnectOutput(wire8);
            wire8.ConnectOutput(wire9);
            wire9.ConnectOutput(wire10);
            Assert.AreEqual(15, wire10.data);
            var vfs = 0;
            wire10.Subscribe((ref int data) =>
            {
                vfs = data;
            });
            Assert.AreEqual(15, vfs);
            wire1.SetData(443);
            Assert.AreEqual(443, vfs);
        }
        
        [Test]
        public void TwoWiresWithReconnect()
        {
            var wire1 = new Wire<int>(15);
            var wire2 = new Wire<int>();
            wire1.ConnectOutput(wire2);
            Assert.AreEqual(15, wire2.data);
            wire1.SetData(16);
            Assert.AreEqual(16, wire2.data);
            wire2.DisconnectInput();
            Assert.AreEqual(0, wire2.data);
            wire1.ConnectOutput(wire2);
            Assert.AreEqual(16, wire2.data);
            wire1.SetData(17);
            Assert.AreEqual(17, wire2.data);
        }
        
        [Test]
        public void TwoWiresWithReconnectAndReconnectEvent()
        {
            var wire1 = new Wire<int>(15);
            var wire2 = new Wire<int>();
            var reconnectCount = 0;
            wire2.OnReconnect += () =>
            {
                reconnectCount++;
            };
            Assert.AreEqual(0, reconnectCount);
            wire1.ConnectOutput(wire2);
            Assert.AreEqual(1, reconnectCount);
            Assert.AreEqual(15, wire2.data);
            wire1.SetData(16);
            Assert.AreEqual(16, wire2.data);
            wire2.DisconnectInput();
            Assert.AreEqual(0, wire2.data);
            Assert.AreEqual(2, reconnectCount);
            wire1.ConnectOutput(wire2);
            Assert.AreEqual(16, wire2.data);
            Assert.AreEqual(3, reconnectCount);
            wire1.SetData(17);
            Assert.AreEqual(17, wire2.data);
            Assert.AreEqual(3, reconnectCount);
        }
        
        [Test]
        public void TwoStepConnectionChangeSource() {
            var wire1 = new Wire<int>(15);
            var wire2 = new Wire<int>(33);
            var wire3 = new Wire<int>();
            var vfs = 0;
            wire3.Subscribe((ref int data) =>
            {
                vfs = data;
            });
            Assert.AreEqual(0, vfs);
            wire3.ConnectInput(wire1);
            Assert.AreEqual(15, wire3.data);
            Assert.AreEqual(15, vfs);
            wire3.ConnectInput(wire2);
            Assert.AreEqual(33, wire3.data);
            Assert.AreEqual(33, vfs);
        }

        [Test]
        public void ThreeWiresAndSetDataAtTheMiddle()
        {
            var wire1 = new Wire<int>(15);
            var wire2 = new Wire<int>();
            var wire3 = new Wire<int>();
            wire1.ConnectOutput(wire2);
            wire2.ConnectOutput(wire3);
            Assert.AreEqual(15, wire3.data);
            wire2.SetData(16); // this should not change wire3 data, because wire2 is not source wire
            Assert.AreEqual(15, wire3.data);
        }
        
        [Test]
        public void DisposeSourceWire()
        {
            var wire1 = new Wire<int>(15);
            var wire2 = new Wire<int>(3);
            wire1.ConnectOutput(wire2);
            Assert.AreEqual(15, wire2.data);
            wire1.Dispose();
            Assert.AreEqual(3, wire2.data);
        }

        [Test]
        public void DisposeMiddleWire()
        {
            var wire1 = new Wire<int>(15);
            var wire2 = new Wire<int>(3);
            var wire3 = new Wire<int>(5);
            wire1.ConnectOutput(wire2);
            wire2.ConnectOutput(wire3);
            Assert.AreEqual(15, wire3.data);
            wire2.Dispose();
            Assert.AreEqual(5, wire3.data);
        }

        [Test]
        public void InputOutputSourceWireProps()
        {
            var wire1 = new Wire<int>(15);
            var wire2 = new Wire<int>(3);
            var wire3 = new Wire<int>(5);
            wire1.ConnectOutput(wire2);
            wire2.ConnectOutput(wire3);
            Assert.AreEqual(wire1, wire2.inputWire);
            // Wire1 outputWires should contain wire2
            Assert.AreEqual(1, wire1.outputWires.Length);
            Assert.AreEqual(wire2, wire1.outputWires[0]);
            // Wire2 outputWires should contain wire3
            Assert.AreEqual(1, wire2.outputWires.Length);
            Assert.AreEqual(wire3, wire2.outputWires[0]);
            // Wire3 outputWires should be empty
            Assert.AreEqual(0, wire3.outputWires.Length);
            // Wire1 sourceWire should be null
            Assert.AreEqual(null, wire1.sourceWire);
        }

        [Test]
        public void PerformanceTest()
        {
            var stopwatch = new Stopwatch();
            var wire1 = new Wire<TestState>(TestState.getRandomValues());
            var wire2 = new Wire<TestState>();
            wire2.ConnectInput(wire1);
            var vfs = new TestState();
            wire2.Subscribe((ref TestState data) =>
            {
                vfs = data;
            });
            var testCases = 100000; // expect 1 ms
            var testStates = new TestState[testCases];
            for (var i = 0; i < testCases; i++)
            {
                testStates[i] = TestState.getRandomValues();
            }
            stopwatch.Start();
            /* Set data */
            for (var i = 0; i < testCases; i++)
            {
                var nextState = testStates[i];
                wire1.Mutate((ref TestState state) =>
                {
                    state.intValue = nextState.intValue;
                    state.bootValue = nextState.bootValue;
                    state.stringValue = nextState.stringValue;
                    state.floatValue = nextState.floatValue;
                    state.vector2Value = nextState.vector2Value;
                    state.vector3Value = nextState.vector3Value;
                    state.vector4Value = nextState.vector4Value;
                    state.quaternionValue = nextState.quaternionValue;
                    state.dateTimeValue = nextState.dateTimeValue;
                    state.timeSpanValue = nextState.timeSpanValue;
                    state.guidValue = nextState.guidValue;
                    state.byteValue = nextState.byteValue;
                    state.sbyteValue = nextState.sbyteValue;
                    state.shortValue = nextState.shortValue;
                    state.ushortValue = nextState.ushortValue;
                    state.uintValue = nextState.uintValue;
                });
            }
            stopwatch.Stop();
            var fps = 1000f / stopwatch.ElapsedMilliseconds;
            Debug.Log($"Performance result ms: {stopwatch.ElapsedMilliseconds}, fps: {fps}");
            Assert.AreEqual(vfs.intValue, testStates[testCases - 1].intValue);
            Assert.LessOrEqual(stopwatch.ElapsedMilliseconds, 12, $"Метод выполняется слишком долго ({stopwatch.ElapsedMilliseconds}ms, {fps} fps)");
        }
        
        [Test]
        public void StructPerformanceTest()
        {
            var stopwatch = new Stopwatch();
            var wire1 = new Wire<TestStructState>(TestStructState.getRandomValues());
            var wire2 = new Wire<TestStructState>();
            wire2.ConnectInput(wire1);
            var vfs = new TestStructState();
            wire2.Subscribe((ref TestStructState data) =>
            {
                vfs = data;
            });
            var testCases = 100000; // expect 1 ms
            var testStates = new TestStructState[testCases];
            for (var i = 0; i < testCases; i++)
            {
                testStates[i] = TestStructState.getRandomValues();
            }
            stopwatch.Start();
            /* Set data */
            for (var i = 0; i < testCases; i++)
            {
                wire1.Mutate((ref TestStructState state) =>
                {
                    ref var nextState = ref testStates[i];
                    state.intValue = nextState.intValue;
                    state.bootValue = nextState.bootValue;
                    state.stringValue = nextState.stringValue;
                    state.floatValue = nextState.floatValue;
                    state.vector2Value = nextState.vector2Value;
                    state.vector3Value = nextState.vector3Value;
                    state.vector4Value = nextState.vector4Value;
                    state.quaternionValue = nextState.quaternionValue;
                    state.dateTimeValue = nextState.dateTimeValue;
                    state.timeSpanValue = nextState.timeSpanValue;
                    state.guidValue = nextState.guidValue;
                    state.byteValue = nextState.byteValue;
                    state.sbyteValue = nextState.sbyteValue;
                    state.shortValue = nextState.shortValue;
                    state.ushortValue = nextState.ushortValue;
                    state.uintValue = nextState.uintValue;
                });
            }
            stopwatch.Stop();
            var fps = 1000f / stopwatch.ElapsedMilliseconds;
            Debug.Log($"Performance result ms: {stopwatch.ElapsedMilliseconds}, fps: {fps}");
            Assert.AreEqual(vfs.intValue, testStates[testCases - 1].intValue);
            Assert.LessOrEqual(stopwatch.ElapsedMilliseconds, 12, $"Метод выполняется слишком долго ({stopwatch.ElapsedMilliseconds}ms, {fps} fps)");
        }
        
        [Test]
        public void ImmutableStructPerformanceTest()
        {
            var stopwatch = new Stopwatch();
            var wire1 = new Wire<TestStructState>(TestStructState.getRandomValues());
            var wire2 = new Wire<TestStructState>();
            wire2.ConnectInput(wire1);
            var vfs = new TestStructState();
            wire2.Subscribe((ref TestStructState data) =>
            {
                vfs = data;
            });
            var testCases = 100000; // expect 1 ms
            var testStates = new TestStructState[testCases];
            for (var i = 0; i < testCases; i++)
            {
                testStates[i] = TestStructState.getRandomValues();
            }
            stopwatch.Start();
            /* Set data */
            for (var i = 0; i < testCases; i++)
            {
                wire1.SetData( testStates[i]);
            }
            stopwatch.Stop();
            var fps = 1000f / stopwatch.ElapsedMilliseconds;
            Debug.Log($"Performance result ms: {stopwatch.ElapsedMilliseconds}, fps: {fps}");
            Assert.AreEqual(vfs.intValue, testStates[testCases - 1].intValue);
            Assert.LessOrEqual(stopwatch.ElapsedMilliseconds, 12, $"Метод выполняется слишком долго ({stopwatch.ElapsedMilliseconds}ms, {fps} fps)");
        }
        
        [Test]
        public void IntValuePerformanceTest()
        {
            var stopwatch = new Stopwatch();
            var wire1 = new Wire<int>(0);
            var wire2 = new Wire<int>();
            wire2.ConnectInput(wire1);
            var vfs = 0;
            wire2.Subscribe((ref int data) =>
            {
                vfs = data;
            });
            var testCases = 100000; // expect 1 ms
            stopwatch.Start();
            /* Set data */
            for (var i = 0; i < testCases; i++)
            {
                wire1.SetData( i );
            }
            stopwatch.Stop();
            var fps = 1000f / stopwatch.ElapsedMilliseconds;
            Debug.Log($"Performance result ms: {stopwatch.ElapsedMilliseconds}, fps: {fps}");
            Assert.AreEqual(vfs, testCases - 1);
            Assert.LessOrEqual(stopwatch.ElapsedMilliseconds, 1, $"Метод выполняется слишком долго ({stopwatch.ElapsedMilliseconds}ms, {fps} fps)");
        }
        
        [Test]
        public void HugeImmutableStructPerformanceTest()
        {
            var stopwatch = new Stopwatch();
            var wire1 = new Wire<HugeTestStructState>(HugeTestStructState.getRandomValues());
            var wire2 = new Wire<HugeTestStructState>();
            wire2.ConnectInput(wire1);
            var vfs = new HugeTestStructState();
            wire2.Subscribe((ref HugeTestStructState data) =>
            {
                vfs = data;
            });
            var testCases = 100000; // expect 1 ms
            var testStates = new HugeTestStructState[testCases];
            for (var i = 0; i < testCases; i++)
            {
                testStates[i] = HugeTestStructState.getRandomValues();
            }
            stopwatch.Start();
            /* Set data */
            for (var i = 0; i < testCases; i++)
            {
                wire1.SetData( testStates[i]);
            }
            stopwatch.Stop();
            var fps = 1000f / stopwatch.ElapsedMilliseconds;
            Debug.Log($"Performance result ms: {stopwatch.ElapsedMilliseconds}, fps: {fps}");
            Assert.AreEqual(vfs.intValue, testStates[testCases - 1].intValue);
            Assert.LessOrEqual(stopwatch.ElapsedMilliseconds, 12, $"Метод выполняется слишком долго ({stopwatch.ElapsedMilliseconds}ms, {fps} fps)");
        }
    }
}