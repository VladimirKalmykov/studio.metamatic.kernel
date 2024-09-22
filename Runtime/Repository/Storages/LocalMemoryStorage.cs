using System;
using System.Collections.Concurrent;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.Storages
{
    public class LocalMemoryStorage : Storage
    {
        // Статический словарь для хранения данных в памяти
        private static readonly ConcurrentDictionary<string, string> _storage = new ConcurrentDictionary<string, string>();

        // Конструктор
        public LocalMemoryStorage(IStorage.TypeVersionControl typeVersionControl = IStorage.TypeVersionControl.Auto)
            : base(typeVersionControl)
        {
        }

        // Асинхронное чтение данных по адресу
        protected override UniTask<string> ReadFile(string address, CancellationToken token)
        {
            UniTask<string> ReadContent()
            {
                token.ThrowIfCancellationRequested();

                if (_storage.TryGetValue(address, out var content))
                {
                    return UniTask.FromResult(content);
                }
                else
                {
                    return UniTask.FromException<string>(new NotFoundException($"Адрес '{address}' не найден в LocalMemoryStorage."));
                }
            }

            return ReadContent();
        }

        // Асинхронная запись данных по адресу
        protected override UniTask WriteFile(string address, string value, CancellationToken token)
        {
            async UniTask WriteContent()
            {
                token.ThrowIfCancellationRequested();

                // Обновляем или добавляем значение в словарь
                _storage[address] = value;

                await UniTask.CompletedTask;
            }

            return WriteContent();
        }

        // Асинхронное удаление данных по адресу
        protected override UniTask DeleteFile(string address, CancellationToken token)
        {
            async UniTask DeleteContent()
            {
                token.ThrowIfCancellationRequested();

                _storage.TryRemove(address, out _);

                await UniTask.CompletedTask;
            }

            return DeleteContent();
        }

        // Асинхронная проверка существования данных по адресу
        protected override UniTask<bool> FileExists(string address, CancellationToken token)
        {
            UniTask<bool> CheckExists()
            {
                token.ThrowIfCancellationRequested();

                return UniTask.FromResult(_storage.ContainsKey(address));
            }

            return CheckExists();
        }
    }
}
