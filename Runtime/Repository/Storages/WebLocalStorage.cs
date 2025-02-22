using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Metamatic.Storages
{
    public class WebLocalStorage : Storage
    {
        // Конструктор
        public WebLocalStorage(IStorage.TypeVersionControl typeVersionControl = IStorage.TypeVersionControl.Orchard)
            : base(typeVersionControl)
        {
        }

        // Проверяем, что код компилируется только для платформы WebGL
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void WebLocalStorage_SetItem(string key, string value);

        [DllImport("__Internal")]
        private static extern IntPtr WebLocalStorage_GetItem(string key);

        [DllImport("__Internal")]
        private static extern void WebLocalStorage_RemoveItem(string key);

        [DllImport("__Internal")]
        private static extern bool WebLocalStorage_HasKey(string key);
#else
        // Для остальных платформ можно использовать PlayerPrefs в качестве замены или выбрасывать исключение
        private static void WebLocalStorage_SetItem(string key, string value)
        {
            throw new PlatformNotSupportedException("WebLocalStorage is only supported on WebGL platform.");
        }

        private static IntPtr WebLocalStorage_GetItem(string key)
        {
            throw new PlatformNotSupportedException("WebLocalStorage is only supported on WebGL platform.");
        }

        private static void WebLocalStorage_RemoveItem(string key)
        {
            throw new PlatformNotSupportedException("WebLocalStorage is only supported on WebGL platform.");
        }

        private static bool WebLocalStorage_HasKey(string key)
        {
            throw new PlatformNotSupportedException("WebLocalStorage is only supported on WebGL platform.");
        }
#endif

        protected override UniTask<string> ReadFile(string address, CancellationToken token)
        {
            async UniTask<string> ReadContent()
            {
                token.ThrowIfCancellationRequested();

                string key = $"{address}.json";

                string value = null;

#if UNITY_WEBGL && !UNITY_EDITOR
                IntPtr ptr = WebLocalStorage_GetItem(key);
                if (ptr == IntPtr.Zero)
                {
                    throw new NotFoundException($"Key '{key}' not found in WebLocalStorage.");
                }
                else
                {
                    value = Marshal.PtrToStringUTF8(ptr);
                    Marshal.FreeHGlobal(ptr); // Освобождаем неуправляемую память
                }
#else
                throw new PlatformNotSupportedException("WebLocalStorage is only supported on WebGL platform.");
#endif

                await UniTask.CompletedTask;
                return value;
            }

            return ReadContent();
        }

        protected override UniTask WriteFile(string address, string value, CancellationToken token)
        {
            async UniTask WriteContent()
            {
                token.ThrowIfCancellationRequested();

                string key = $"{address}.json";

                WebLocalStorage_SetItem(key, value);

                await UniTask.CompletedTask;
            }

            return WriteContent();
        }

        protected override UniTask DeleteFile(string address, CancellationToken token)
        {
            async UniTask DeleteContent()
            {
                token.ThrowIfCancellationRequested();

                string key = $"{address}.json";

                WebLocalStorage_RemoveItem(key);

                await UniTask.CompletedTask;
            }

            return DeleteContent();
        }

        protected override UniTask<bool> FileExists(string address, CancellationToken token)
        {
            async UniTask<bool> CheckExists()
            {
                token.ThrowIfCancellationRequested();

                string key = $"{address}.json";

                bool exists;

#if UNITY_WEBGL && !UNITY_EDITOR
                exists = WebLocalStorage_HasKey(key);
#else
                exists = WebLocalStorage_HasKey(key);
#endif

                await UniTask.CompletedTask;
                return exists;
            }

            return CheckExists();
        }
    }
}
