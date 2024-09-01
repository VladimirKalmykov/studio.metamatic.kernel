using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Metamatic.EditorInspector
{
    public static class StaticLogsCollector
    {
        private static readonly List<LogMetadata> m_LogHistory = new ();
        public static event Action<LogMetadata> OnLogReceived;
        private static Timer m_Timer;
        private static MicroState<bool> BuiltInLogEnabled = new();
        public static MicroStateReadOnly<bool> builtInLogEnabled => BuiltInLogEnabled;
        public static bool isBuiltInLogEnabled => BuiltInLogEnabled.value;
        public static IReadOnlyList<LogMetadata> logHistory => m_LogHistory;
        
        public static void ChangeBuiltInLogEnable(bool enable)
        {
            BuiltInLogEnabled.value = enable;
            PlayerPrefs.SetInt("MetamaticConsole_BuiltInLogEnabled", enable ? 1 : 0);
        }
        
        static StaticLogsCollector()
        {
            SubscribeToSources();
            BuiltInLogEnabled.value = PlayerPrefs.GetInt("MetamaticConsole_BuiltInLogEnabled", 1) == 1;
            m_Timer = new  Timer((state) => OnTimerCheck(), null,
                10000, 10000);
        }

        private static void SubscribeToSources()
        {
            Application.logMessageReceived += OnBuiltInLogReceived;
            Console.OnLog += OnMetamaticLogReceived;
        }
        
        private static void UnsubscribeFromSources()
        {
            Application.logMessageReceived -= OnBuiltInLogReceived;
            Console.OnLog -= OnMetamaticLogReceived;
        }
        
        /// <summary>
        /// Timer check for subscription existence. If no subscribers then unsubscribe from all log sources.
        /// </summary>
        private static void OnTimerCheck()
        {
            if (OnLogReceived == null)
            {
                UnsubscribeFromSources();
                m_Timer.Dispose();
                m_LogHistory.Clear();
            }
        }
        
        private static void OnMetamaticLogReceived(string text, string stackTrace, Console.LogLevel logLevel)
        {
            var (extractedTags, textWithoutTags) = ExtractTagsFromText(text, logLevel.ToString());
            var logMetadata = new LogMetadata(textWithoutTags, extractedTags, false, stackTrace);
            m_LogHistory.Add(logMetadata);
            OnLogReceived?.Invoke(logMetadata);
        }
        
        /// <summary>
        /// Handler for built-in log messages
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="stackTrace"></param>
        /// <param name="type"></param>
        private static void OnBuiltInLogReceived(string condition, string stackTrace, LogType type)
        {
            var (extractedTags, text) = ExtractTagsFromText(condition, type.ToString());
            var logMetadata = new LogMetadata(text, extractedTags, true, stackTrace);
            m_LogHistory.Add(logMetadata);
            OnLogReceived?.Invoke(logMetadata);
        }
        
        /// <summary>
        /// Extract tags from log text
        /// </summary>
        /// <param name="text"></param>
        /// <param name="staticTag"></param>
        /// <returns></returns>
        private static (string[], string) ExtractTagsFromText(string text, string staticTag)
        {
            var tags = new List<string>();
            if (!string.IsNullOrEmpty(staticTag))
            {
                tags.Add(staticTag);
            }
            var tagStart = text.IndexOf("[", StringComparison.Ordinal);
            while (tagStart != -1)
            {
                var tagEnd = text.IndexOf("]", tagStart, StringComparison.Ordinal);
                if (tagEnd == -1)
                {
                    break;
                }
                tags.Add(text.Substring(tagStart + 1, tagEnd - tagStart - 1));
                text = text.Remove(tagStart, tagEnd - tagStart + 1);
                tagStart = text.IndexOf("[", tagStart, StringComparison.Ordinal);
            }

            return (tags.ToArray(), text);
        }
        
        #region Public Methods
        public static DisposableAction SubscribeToLogHistory(Action<LogMetadata> onLogReceived)
        {
            /* Send all log history to the subscriber */
            foreach (var metadata in m_LogHistory)
            {
                onLogReceived(metadata);
            }
            void Handler(LogMetadata metadata)
            {
                onLogReceived(metadata);
            }

            if (OnLogReceived == null)
            {
                SubscribeToSources();
            }
            OnLogReceived += Handler;
            return new DisposableAction(() => OnLogReceived -= Handler);
        }
        
        public static void ClearLogHistory()
        {
            m_LogHistory.Clear();
        }
        #endregion
    }
}