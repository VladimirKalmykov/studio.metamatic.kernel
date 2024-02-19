using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Task = UnityEditor.VersionControl.Task;
using UniTask = Cysharp.Threading.Tasks.UniTask;

namespace Metamatic
{
    public class FsJsonSource: IOSource
    {
        public override AsyncReadOperation<T> Read<T>(string filePath, CancellationToken token)
        {
            var operationSource = new AsyncReadOperationSource<T>();
            string fullPath = Path.Combine(Application.persistentDataPath, $"{filePath}.json");
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

        public override AsyncWriteOperation<T> Write<T>(string uri, T value, CancellationToken token)
        {
            var operationSource = new AsyncWriteOperationSource<T>();
            string fullPath = Path.Combine(Application.persistentDataPath, $"{uri}.json");
            var package = new SerializableDataPackage<T>(value);
            string json = JsonUtility.ToJson(package);
            operationSource.SetProgress(0);
            UniTask.Run(async () =>
            {
                
                try
                {
                    token.ThrowIfCancellationRequested();
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
    }
}