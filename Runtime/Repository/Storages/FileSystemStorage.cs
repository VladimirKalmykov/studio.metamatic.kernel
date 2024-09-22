using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Storages
{
    [PlatformCompatible(RuntimePlatform.WindowsPlayer)]
    [PlatformCompatible(RuntimePlatform.WindowsEditor)]
    public class FileSystemStorage : Storage
    {
        #region Static

        // Добавляем словарь для хранения семафоров
        private readonly ConcurrentDictionary<string, SemaphoreSlim> m_FileSemaphores =
            new ConcurrentDictionary<string, SemaphoreSlim>();

        #endregion

        public readonly string PersistencePath;

        public FileSystemStorage(string persistencePath = null,
            IStorage.TypeVersionControl typeVersionControl = IStorage.TypeVersionControl.Auto) : base(
            typeVersionControl)
        {
            PersistencePath = persistencePath ?? Application.persistentDataPath;
        }

        protected override UniTask<string> ReadFile(string address, CancellationToken token)
        {
            string fullPath = Path.Combine(PersistencePath, $"{address}.json");
#if METAMATIC_VERBOSE_LOG
            Console.Verbose("FileSystemStorage", "ReadFile", fullPath);
#endif
            // Получаем семафор для данного файла
            var semaphore = m_FileSemaphores.GetOrAdd(fullPath, new SemaphoreSlim(1, 1));
            
            async UniTask<string> ReadFileContent()
            {
                await semaphore.WaitAsync(token); // Ожидаем освобождения семафора
                try
                {
                    token.ThrowIfCancellationRequested();

                    string fileContent;
                    using StreamReader reader = new StreamReader(fullPath);
                    fileContent = await reader.ReadToEndAsync();
                    token.ThrowIfCancellationRequested();

                    return fileContent;
                }
                catch (Exception e)
                {
                    if (e is DirectoryNotFoundException || e is FileNotFoundException)
                    {
                        throw new NotFoundException($"{e.GetType().Name}");
                    }
                    else
                    {
                        throw;
                    }
                }
                finally
                {
                    semaphore.Release(); // Освобождаем семафор

                    // Попытка удалить семафор из словаря, если он не используется
                    if (semaphore.CurrentCount == 1) // Проверяем, что нет ожидания на семафор
                    {
                        m_FileSemaphores.TryRemove(fullPath, out _);
                    }
                }
            }
            
            return UniTask.RunOnThreadPool(ReadFileContent, cancellationToken: token, configureAwait: true);
        }

        protected override UniTask WriteFile(string address, string value, CancellationToken token)
        {
            string fullPath = Path.Combine(PersistencePath, $"{address}.json");
            #if METAMATIC_VERBOSE_LOG
            Console.Verbose("FileSystemStorage", "WriteFile", fullPath);
            #endif
            // Получаем семафор для данного файла
            var semaphore = m_FileSemaphores.GetOrAdd(fullPath, new SemaphoreSlim(1, 1));

            async UniTask WriteFileContent()
            {
                await semaphore.WaitAsync(token); // Ожидаем освобождения семафора
                try
                {
                    token.ThrowIfCancellationRequested();

                    // Создаем все недостающие директории для fullPath
                    string directoryPath = Path.GetDirectoryName(fullPath);
                    if (!string.IsNullOrEmpty(directoryPath))
                    {
                        token.ThrowIfCancellationRequested();
                        Directory.CreateDirectory(directoryPath); // Создаем директории
                    }
                    token.ThrowIfCancellationRequested();
                    await using (StreamWriter writer = new StreamWriter(fullPath, false))
                    {
                        token.ThrowIfCancellationRequested();
                        await writer.WriteAsync(value);
                    }
                }
                finally
                {
                    semaphore.Release(); // Освобождаем семафор в любом случае

                    // Попытка удалить семафор из словаря, если он не используется
                    if (semaphore.CurrentCount == 1) // Проверяем, что нет ожидания на семафор
                    {
                        m_FileSemaphores.TryRemove(fullPath, out _);
                    }
                }
            }

            return UniTask.RunOnThreadPool(WriteFileContent, true, token);
        }

        protected override UniTask DeleteFile(string address, CancellationToken token)
        {
            string fullPath = Path.Combine(PersistencePath, $"{address}.json");
#if METAMATIC_VERBOSE_LOG
            Console.Verbose("FileSystemStorage", "DeleteFile", fullPath);
#endif
            // Получаем семафор для данного файла
            var semaphore = m_FileSemaphores.GetOrAdd(fullPath, new SemaphoreSlim(1, 1));

            async UniTask DeleteFile()
            {
                await semaphore.WaitAsync(token); // Ожидаем освобождения семафора
                try
                {
                    token.ThrowIfCancellationRequested();

                    if (File.Exists(fullPath))
                    {
                        token.ThrowIfCancellationRequested();
                        File.Delete(fullPath);
                    }
                }
                finally
                {
                    semaphore.Release();

                    // Попытка удалить семафор из словаря, если он не используется
                    if (semaphore.CurrentCount == 1) // Проверяем, что нет ожидания на семафор
                    {
                        m_FileSemaphores.TryRemove(fullPath, out _);
                    }
                }
            }

            return UniTask.RunOnThreadPool(DeleteFile, true, token);
        }

        protected override UniTask<bool> FileExists(string address, CancellationToken token)
        {
            string fullPath = Path.Combine(PersistencePath, $"{address}.json");

            // Получаем семафор для данного файла
            var semaphore = m_FileSemaphores.GetOrAdd(fullPath, new SemaphoreSlim(1, 1));

            async UniTask<bool> CheckFileExists()
            {
                await semaphore.WaitAsync(token); // Ожидаем освобождения семафора
                try
                {
                    token.ThrowIfCancellationRequested();

                    return File.Exists(fullPath);
                }
                finally
                {
                    semaphore.Release();

                    // Попытка удалить семафор из словаря, если он не используется
                    if (semaphore.CurrentCount == 1) // Проверяем, что нет ожидания на семафор
                    {
                        m_FileSemaphores.TryRemove(fullPath, out _);
                    }
                }
            }

            return UniTask.RunOnThreadPool(CheckFileExists, cancellationToken: token, configureAwait: true);
        }
        
        
    }
}