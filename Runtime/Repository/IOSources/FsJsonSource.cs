using System;
using System.IO;
using System.Threading;
using UnityEngine;
using UniTask = Cysharp.Threading.Tasks.UniTask;

namespace Metamatic
{
    public class FsJsonSource: IOSource
    {
        public override AsyncReadOperation<T> Read<T>(string name, CancellationToken token)
        {
            var operationSource = new AsyncReadOperationSource<T>();
            var uri = GetUriAddress<T>(name);
            string fullPath = Path.Combine(Application.persistentDataPath, uri);
            // Запускаем асинхронную операцию чтения файла
            UniTask.Run(async delegate ()
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
                    operationSource.SetRawValue(fileContent);
                }
                catch (Exception e)
                {
                    await UniTask.SwitchToMainThread();
                    operationSource.SetError(e);
                }
            }, cancellationToken: token);

            return operationSource.Operation;
        }

        public override AsyncWriteOperation<T> Write<T>(string name, T value, CancellationToken token)
        {
            var operationSource = new AsyncWriteOperationSource<T>();
            var uri = GetUriAddress<T>(name);
            string fullPath = Path.Combine(Application.persistentDataPath, uri);
            var package = new SerializableDataPackage<T>(value);
            string json = JsonUtility.ToJson(package);
            operationSource.SetProgress(0);
            UniTask.Run(async () =>
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
                    operationSource.SetCompleted();
                }
                catch (Exception e)
                {
                    await UniTask.SwitchToMainThread();
                    operationSource.SetError(e);
                }
            });
            return operationSource.Operation;
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