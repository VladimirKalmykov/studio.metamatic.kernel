using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic
{
    [PlatformCompatible(RuntimePlatform.WindowsPlayer)]
    [PlatformCompatible(RuntimePlatform.WindowsEditor)]
    public class FsJsonStorage: Storage
    {
        public override UniTask<(TData, Metadata, TMetadata)> Get<TData, TMetadata>(string address, CancellationToken token)
        {
            if (!enabled)
            {
                throw new Storage.DisabledException();
            }
            
            var taskCompletionSource = new UniTaskCompletionSource<(TData, Storage.Metadata, TMetadata)>();
            var uri = GetUriAddress<TData>(address);
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
                    token.ThrowIfCancellationRequested();
                    taskCompletionSource.TrySetResult(Storage.Deserialize<TData, TMetadata>(fileContent));
                }
                catch (Exception e)
                {
                    if (e is DirectoryNotFoundException || e is FileNotFoundException)
                    {
                        /* Anyway synchronized because it has no persistent storage record, it is normal scenario */
                        await UniTask.SwitchToMainThread();
                        taskCompletionSource.TrySetException(new Storage.NotFoundException($"{e.GetType().Name}"));
                    }
                    else
                    {
                        taskCompletionSource.TrySetException(e);
                    }
                }
                finally
                {
                    
                    await UniTask.SwitchToMainThread();
                }
            }, cancellationToken: token);

            return taskCompletionSource.Task;
        }

        public override UniTask Set<TData, TMetadata>(string address, TData value, TMetadata customMetadata, CancellationToken token)
        {
            if (!enabled)
            {
                throw new Storage.DisabledException();
            }
            var taskCompletionSource = new UniTaskCompletionSource();
            var uri = GetUriAddress<TData>(address);
            string fullPath = Path.Combine(Application.persistentDataPath, uri);
            string json = Serialize(value, customMetadata);
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
                    token.ThrowIfCancellationRequested();
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

        public override UniTask Delete<TData>(string address, CancellationToken token)
        {
            if (!enabled)
            {
                throw new Storage.DisabledException();
            }
            var taskCompletionSource = new UniTaskCompletionSource();
            var uri = GetUriAddress<TData>(address);
            string fullPath = Path.Combine(Application.persistentDataPath, uri);
            UniTask.RunOnThreadPool(async () =>
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                    }
                    await UniTask.SwitchToMainThread();
                    token.ThrowIfCancellationRequested();
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
            /* var typeName = typeof(T).FullName;
            return $"{typeName}/{uri}.json"; */
            string typeName = typeof(T).FullName;
            string input = $"{typeName}:{uri}";

            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] data = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sb.Append(data[i].ToString("x2"));
                }

                return sb.ToString();
            }
        }

    }
}