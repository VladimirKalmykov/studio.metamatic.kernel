using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Metamatic.Storages;
using NUnit.Framework;
using UnityEngine;
using System.Collections;
using Metamatic;
using NUnit.Framework.Internal;
using UnityEngine.TestTools;
using Console = System.Console;
using LocalMemoryStorage = Metamatic.Storages.LocalMemoryStorage;

public class LocalMemoryStorageTests
{
    private struct TestData
    {
        public string text;
        public int number;
    
        /* Переопределение метода Equals для сравнения значений полей */
        public override bool Equals(object obj)
        {
            if (obj is TestData other)
            {
                return text == other.text && number == other.number;
            }
            return false;
        }

        /* Переопределение метода GetHashCode для корректной работы хеш-таблиц */
        public override int GetHashCode()
        {
            return text.GetHashCode() ^ number.GetHashCode();
        }

        /* Перегрузка оператора == для структурного сравнения */
        public static bool operator ==(TestData left, TestData right)
        {
            return left.Equals(right);
        }

        /* Перегрузка оператора != для структурного сравнения */
        public static bool operator !=(TestData left, TestData right)
        {
            return !(left == right);
        }
    }
    [Serializable]
    [Version("0.1")]
    private struct VersionedTestData
    {
        public string text;
        public int number;
    }
    private string testDirectory;
    private LocalMemoryStorage storage;

    [SetUp]
    public void SetUp()
    {
        // Создаём временную директорию для тестов
        testDirectory = Path.Combine(Application.temporaryCachePath, "FileSystemStorageTests", Path.GetRandomFileName());
        Directory.CreateDirectory(testDirectory);
        Debug.Log($"testDirectory: {testDirectory}");

        // Создаём экземпляр FileSystemStorage с использованием временной директории
        storage = new LocalMemoryStorage();
    }

    [TearDown]
    public void TearDown()
    {
        // Удаляем временную директорию после выполнения тестов
        if (Directory.Exists(testDirectory))
        {
            Directory.Delete(testDirectory, true);
        }
    }

    [UnityTest]
    public IEnumerator TestWriteAndRead() => UniTask.ToCoroutine(async () =>
    {
        string address = "testFile";
        TestData data = new TestData() { text = "Hello, World!", number = 42 };

        // Записываем данные
        await storage.Set<TestData>(address, data, CancellationToken.None);

        // Читаем данные
        TestData result = await storage.Get<TestData>(address, CancellationToken.None);

        // Проверяем, что записанные и прочитанные данные совпадают
        Assert.AreEqual(data.text, result.text);
        Assert.AreEqual(data.number, result.number);
    });

    [UnityTest]
    public IEnumerator TestConcurrentWrites() => UniTask.ToCoroutine(async () =>
    {
        string address = "testFile";
        var data1 = new TestData() { text = "Hello, World 1!", number = 1 };
        var data2 = new TestData() { text = "Hello, World 2!", number = 2 };

        var cts = new CancellationTokenSource();

        // Запускаем две операции записи одновременно
        var task1 = storage.Set<TestData>(address, data1, cts.Token);
        var task2 = storage.Set<TestData>(address, data2, cts.Token);

        // Ожидаем завершения обеих операций
        await UniTask.WhenAll(task1, task2);

        // Читаем данные
        TestData result = await storage.Get<TestData>(address, CancellationToken.None);

        // Проверяем, что файл содержит data2
        Assert.IsTrue(result == data2);
    });

    [UnityTest]
    public IEnumerator TestConcurrentReadAndWrite() => UniTask.ToCoroutine(async () =>
    {
        string address = "testFile";
        TestData initialData = new TestData() { text = "Initial data!", number = 1 };
        TestData newData = new TestData() { text = "New data!", number = 2 };

        // Записываем начальные данные
        await storage.Set<TestData>(address, initialData, CancellationToken.None);

        var cts = new CancellationTokenSource();

        // Запускаем операцию чтения и операцию записи одновременно
        var readTask = storage.Get<TestData>(address, cts.Token);
        var writeTask = storage.Set<TestData>(address, newData, cts.Token);

        // Ожидаем завершения обеих операций
        await UniTask.WhenAll(readTask, writeTask);

        // Читаем данные после записи
        TestData result = await storage.Get<TestData>(address, CancellationToken.None);

        // Проверяем, что данные были обновлены
        Assert.AreEqual(newData, result);
    });

    [UnityTest]
    public IEnumerator TestCancellation() => UniTask.ToCoroutine(async () =>
    {
        string address = "testFile";
        TestData data = new TestData() { text = "Never", number = -1 };

        var cts = new CancellationTokenSource();

        // Отменяем операцию сразу
        cts.Cancel();

        // Пытаемся записать данные
        try
        {
            await storage.Set<TestData>(address, data, cts.Token);
            Assert.Fail("Ожидалось исключение OperationCanceledException");
        }
        catch (OperationCanceledException)
        {
            // Операция была отменена, всё корректно
        }

        // Проверяем, что файл не был создан
        var exists = await storage.Exists<TestData>(address, CancellationToken.None);
        
        Assert.IsFalse(exists);
    });

    [UnityTest]
    public IEnumerator TestNotFound() => UniTask.ToCoroutine(async () =>
    {
        string address = "nonexistentFile";

        // Пытаемся прочитать несуществующий файл
        try
        {
            await storage.Get<TestData>(address, CancellationToken.None);
            Assert.Fail("Ожидалось исключение NotFoundException");
        }
        catch (Storage.NotFoundException)
        {
            // Файл не найден, всё корректно
        }
    });

    [UnityTest]
    public IEnumerator TestDisableStorage() => UniTask.ToCoroutine(async () =>
    {
        string address = "testFile.txt";
        TestData data = new TestData() { text = "Hello, World!", number = 42 };

        // Отключаем хранилище
        storage.enabled = false;

        // Пытаемся записать данные
        try
        {
            await storage.Set<TestData>(address, data, CancellationToken.None);
            Assert.Fail("Ожидалось исключение DisabledException");
        }
        catch (Storage.DisabledException)
        {
            // Хранилище отключено, всё корректно
        }
    });
    
    [UnityTest]
    public IEnumerator TestVersionedData() => UniTask.ToCoroutine(async () =>
    {
        string address = "testFile";
        VersionedTestData data = new VersionedTestData() { text = "Hello, World!", number = 42 };

        // Записываем данные
        await storage.Set<VersionedTestData>(address, data, CancellationToken.None);

        // Читаем данные
        VersionedTestData result = await storage.Get<VersionedTestData>(address, CancellationToken.None);

        // Проверяем, что записанные и прочитанные данные совпадают
        Assert.AreEqual(data.text, result.text);
        Assert.AreEqual(data.number, result.number);
    });
}
