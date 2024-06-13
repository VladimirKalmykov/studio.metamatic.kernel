using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    [PlatformCompatible(RuntimePlatform.WindowsPlayer)]
    [PlatformCompatible(RuntimePlatform.WindowsEditor)]
    public class FsJsonSource: IOSource
    {
        public override UniTask<T> Read<T>(string name, CancellationToken token)
        {
            var taskCompletionSource = new UniTaskCompletionSource<T>();
            var ioOperation = new IOOperation();
            var uri = GetUriAddress<T>(name);
            string fullPath = Path.Combine(Application.persistentDataPath, uri);
            // Запускаем асинхронную операцию чтения файла
            UniTask.RunOnThreadPool(async delegate ()
            {
                try
                {
                    token.ThrowIfCancellationRequested();

                    string fileContent;
                    using (StreamReader reader = new StreamReader(fullPath))
                    {
                        fileContent = await reader.ReadToEndAsync();
                        token.ThrowIfCancellationRequested();
                    }

                    await UniTask.SwitchToMainThread();
                    ioOperation.progress = 1f;
                    taskCompletionSource.TrySetResult(IOSource.Deserialize<T>(fileContent));
                }
                catch (DirectoryNotFoundException)
                {
                    /* Anyway synchronized because it has no persistent storage record, it is normal scenario */
                    await UniTask.SwitchToMainThread();
                    taskCompletionSource.TrySetException(new IOSource.NotFoundException("Directory not found"));
                }
                catch (FileNotFoundException)
                {
                    /* Anyway synchronized because it has no persistent storage record, it is normal scenario */
                    await UniTask.SwitchToMainThread();
                    taskCompletionSource.TrySetException(new IOSource.NotFoundException("File not found"));
                }
                catch (Exception e)
                {
                    await UniTask.SwitchToMainThread();
                    taskCompletionSource.TrySetException(e);
                }
            }, cancellationToken: token);

            return taskCompletionSource.Task;
        }

        public override UniTask Write<T>(string name, T value, CancellationToken token)
        {
            var taskCompletionSource = new UniTaskCompletionSource();
            var ioOperation = new IOOperation();
            var uri = GetUriAddress<T>(name);
            string fullPath = Path.Combine(Application.persistentDataPath, uri);
            string json = Serialize(value);
            UniTask.RunOnThreadPool(async () =>
            {
                try
                {
                    token.ThrowIfCancellationRequested();

                    // Создаем все недостающие директории для fullPath
                    string directoryPath = Path.GetDirectoryName(fullPath);
                    if (!string.IsNullOrEmpty(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath); // Создаем директории
                    }

                    using (await lockerManager.GetAsyncUriLocker(uri, token))
                    using (StreamWriter writer = new StreamWriter(fullPath, false))
                    {
                        token.ThrowIfCancellationRequested();
                        await writer.WriteAsync(json);
                    }
                    token.ThrowIfCancellationRequested();
                
                    await UniTask.SwitchToMainThread();
                    ioOperation.progress = 1f;
                    taskCompletionSource.TrySetResult();
                }
                catch (Exception e)
                {
                    await UniTask.SwitchToMainThread();
                    taskCompletionSource.TrySetException(e);
                }
            });
            return taskCompletionSource.Task;
        }
        
        /// <summary>
        /// Get data address by its type and uri
        /// </summary>
        /// <param name="uri"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static string GetUriAddress<T>(string uri)
        {
            var typeName = typeof(T).FullName;
            return $"{typeName}/{uri}.json";
        }

    }
}