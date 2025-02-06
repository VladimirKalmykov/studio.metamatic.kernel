using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Metamatic;
using NUnit.Framework;
using Console = System.Console;

[TestFixture]
public class IndexedPoolTests
{
    [Test]
    public void Add_And_Get_ShouldWork()
    {
        // Arrange
        var pool = new IndexedPool<string>(initialCapacity: 2);

        // Act
        int idx = pool.Add("hello");
        string value = pool.Get(idx);

        // Assert
        Assert.AreEqual("hello", value);
    }

    [Test]
    public void Remove_ShouldFreeIndex()
    {
        var pool = new IndexedPool<int>(2);

        int idx = pool.Add(42);
        Assert.AreEqual(42, pool.Get(idx));

        pool.Remove(idx);
        // Тот же индекс должен быть свободен для повторного использования
        int idx2 = pool.Add(100);

        // idx2 должен совпасть с idx, если логика стека свободных индексов отработала
        Assert.AreEqual(idx, idx2);
        Assert.AreEqual(100, pool.Get(idx2));
    }

    [Test]
    public void Resize_ShouldDoubleCapacity()
    {
        var pool = new IndexedPool<int>(1);

        int idx1 = pool.Add(1);
        int idx2 = pool.Add(2); // должно привести к Resize

        Assert.AreEqual(1, pool.Get(idx1));
        Assert.AreEqual(2, pool.Get(idx2));
    }
    
    [Test]
    public void TryGet_ValidIndex_ShouldReturnTrueAndItem()
    {
        var pool = new IndexedPool<string>(2);
        int idx = pool.Add("hello");
    
        bool success = pool.TryGet(idx, out var value);
    
        Assert.IsTrue(success);
        Assert.AreEqual("hello", value);
    }

    [Test]
    public void TryGet_InvalidOrFreedIndex_ShouldReturnFalse()
    {
        var pool = new IndexedPool<int>(2);
        int idx = pool.Add(123);

        // Удаляем индекс
        pool.Remove(idx);

        // Теперь он невалиден: должен вернуть false и default
        bool success = pool.TryGet(idx, out var val);
        Assert.IsFalse(success);
        Assert.AreEqual(default(int), val);

        // Индекс за пределами Count
        success = pool.TryGet(idx + 100, out val);
        Assert.IsFalse(success);
    }

    [Test]
    public void Remove_WithOutParameter_ShouldReturnElement()
    {
        var pool = new IndexedPool<int>(2);
        int idx = pool.Add(999);

        bool removed = pool.Remove(idx, out var removedValue);

        Assert.IsTrue(removed);
        Assert.AreEqual(999, removedValue);

        // Повторный вызов не должен удалять, т.к. индекс уже свободен
        removed = pool.Remove(idx, out removedValue);
        Assert.IsFalse(removed);
    }

    [Test]
    public void Has_UsedIndex_ShouldReturnTrue()
    {
        var pool = new IndexedPool<int>(2);
        int idx = pool.Add(10);

        bool hasIndex = pool.Has(idx);
        Assert.IsTrue(hasIndex);
    }

    [Test]
    public void Has_FreedIndex_ShouldReturnFalse()
    {
        var pool = new IndexedPool<int>(2);
        int idx = pool.Add(42);

        pool.Remove(idx);

        bool hasIndex = pool.Has(idx);
        Assert.IsFalse(hasIndex);
    }
    
    [Test]
    public void AddRemove_Performance_Test()
    {
        // Настраиваем пул
        const int iterations = 1_000_000;
        var pool = new IndexedPool<int>(initialCapacity: 1000000);

        var sw = Stopwatch.StartNew();

        // Массово добавляем
        for (int i = 0; i < iterations; i++)
        {
            pool.Add(i);
        }
        long addTime = sw.ElapsedMilliseconds;

        // Массово удаляем (любые объекты, скажем, все подряд)
        for (int i = 0; i < iterations; i++)
        {
            pool.Remove(i);
        }
        long removeTime = sw.ElapsedMilliseconds - addTime;

        sw.Stop();

        Console.WriteLine($"[IndexedPool] Add({iterations}) = {addTime} ms");
        Console.WriteLine($"[IndexedPool] Remove({iterations}) = {removeTime} ms");
        
        Assert.Less(addTime, 16);
        Assert.Less(removeTime, 16);
    }
    
    [Test]
    public void TestX()
    {
        var coll = new List<string>();
        
        var wordTs = new UniTaskCompletionSource<string>();
        var wordTask = wordTs.Task;
        wordTs.TrySetResult("hello");
        
        Test1(coll, wordTask);
        Test2(coll);

        
        
        Assert.AreEqual(2, coll.Count);
        Assert.AreEqual("hello", coll[0]);
        Assert.AreEqual("world", coll[1]);
    }

    async void Test1(List<string> coll, UniTask<string> wordTask) {
        var word = await wordTask;
        coll.Add(word);
        
    }

    void Test2(List<string> coll) {
        coll.Add("world");
    }
}