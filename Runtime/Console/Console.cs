using System;
using System.Diagnostics;
using System.IO;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace Metamatic
{
    public static class Console
    {
        [Flags]
        public enum LogLevel
        {
            None = 0,
            Verbose = 1 << 1,
            Debug = 1 << 2,
            Info = 1 << 3,
            Warning = 1 << 4,
            Error = 1 << 5,
            Exception = 1 << 6,
            Interactive = 1 << 7,
            Minimal = Error | Exception,
            Standard = Info | Warning | Error | Exception | Interactive,
            Full = Verbose | Debug | Info | Warning | Error | Exception | Interactive
        }

        static Console()
        {
#if !METAMATIC_VERBOSE_LOG
            UnityEngine.Debug.Log("[Console] Verbose logging is disabled. Set METAMATIC_VERBOSE_LOG to true in the project settings to enable it.");
#endif
        }
        
        private static event Action OnSceneInitiallyLoaded;
        private static bool _IsInitialized;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            _IsInitialized = true;
            OnSceneInitiallyLoaded?.Invoke();
            OnSceneInitiallyLoaded = null;
        }
        
        public static readonly Commander Commander = new Commander();
        
        #region Public Properties and Methods
        
        /// <summary>
        /// Default log level for Unity console
        /// </summary>
        public static LogLevel LogLevelTranslateToUnityConsole = LogLevel.Error;
        public static event Action<string, string[], string, LogLevel> OnLog;
        
        [UsedImplicitly]
        [DebuggerHidden]
        public static void LogWithStackTrance(string message, string[] tags, LogLevel logLevel = LogLevel.Info, StackTrace stackTrace = null)
        {
            if (logLevel == LogLevel.None)
            {
                return;
            }

            var finalStackTrace = stackTrace ?? new StackTrace(true);
            LogOnTheMainThread(message, tags, logLevel, finalStackTrace.ToString()).Forget();
        }
        
        [UsedImplicitly]
        [DebuggerHidden]
        public static void Log(string message, string[] tags, LogLevel logLevel = LogLevel.Info, string stackTrace = null)
        {
            if (logLevel == LogLevel.None)
            {
                return;
            }
            var finalStackTrace = stackTrace ?? new StackTrace(true).ToString();
            LogOnTheMainThread(message, tags, logLevel, finalStackTrace).Forget();
        }
        
        [UsedImplicitly]
        [DebuggerHidden]
        public static void Log(string tag1, string message, LogLevel logLevel = LogLevel.Info, string stackTrace = null)
        {
            if (logLevel == LogLevel.None)
            {
                return;
            }
            var finalStackTrace = stackTrace ?? new StackTrace(true).ToString();
            LogOnTheMainThread(message, new []{ tag1 }, logLevel, finalStackTrace).Forget();
        }
        
        [UsedImplicitly]
        [DebuggerHidden]
        public static void Log(string tag1, string tag2, string message, LogLevel logLevel = LogLevel.Info, string stackTrace = null)
        {
            if (logLevel == LogLevel.None)
            {
                return;
            }
            #if !METAMATIC_VERBOSE_LOG
            if (logLevel == LogLevel.Verbose)
            {
                return;
            }
            #endif
            var finalStackTrace = stackTrace ?? new StackTrace(true).ToString();
            LogOnTheMainThread(message, new []{ tag1, tag2 }, logLevel, finalStackTrace).Forget();
        }
        
        [UsedImplicitly]
        [DebuggerHidden]
        public static void Log(string tag1, string tag2, string tag3, string message, LogLevel logLevel = LogLevel.Info, string stackTrace = null)
        {
            if (logLevel == LogLevel.None)
            {
                return;
            }
            var finalStackTrace = stackTrace ?? new StackTrace(true).ToString();
            LogOnTheMainThread(message, new []{ tag1, tag2, tag3 }, logLevel, finalStackTrace).Forget();
        }
        
        [UsedImplicitly]
        [DebuggerHidden]
        public static void Log(string tag1, string tag2, string tag3, string tag4, string message, LogLevel logLevel = LogLevel.Info, string stackTrace = null)
        {
            if (logLevel == LogLevel.None)
            {
                return;
            }
            var finalStackTrace = stackTrace ?? new StackTrace(true).ToString();
            LogOnTheMainThread(message, new []{ tag1, tag2, tag3, tag4 }, logLevel, finalStackTrace).Forget();
        }
        
        [DebuggerHidden]
        private static async UniTaskVoid LogOnTheMainThread(string message, string[] tags, LogLevel logLevel, string stackTrace)
        {
            /* Get current thread */
            var currentThread = System.Threading.Thread.CurrentThread;
            if (currentThread.ManagedThreadId != 1)
            {
                /*
                 Because this method can be called before UniTask actually initialize its `yielders` array (that is used in SwitchToMainThread)
                 we must ensure that UniTask already initialized before ask for switch to main thread.
                 The `yielders` array is initialized on BeforeSceneLoad event.
                 
                 To avoid this issue, we can defer the call to the main thread only after BeforeSceneLoad event.
                 */
                if (!_IsInitialized)
                {
                    OnSceneInitiallyLoaded += () =>
                    {
                        LogOnTheMainThread(message, tags, logLevel, stackTrace).Forget();
                    };
                    return;
                }
                await UniTask.SwitchToMainThread();
            }
            
            if ((LogLevelTranslateToUnityConsole & logLevel) == logLevel)
            {
                if (logLevel == LogLevel.Warning)
                {
                    UnityEngine.Debug.LogWarning(CompileMessageWithTags(tags, message));
                }
                else if (logLevel == LogLevel.Error || logLevel == LogLevel.Exception)
                {
                    UnityEngine.Debug.LogError(CompileMessageWithTags(tags, message));
                }
                else
                {
                    UnityEngine.Debug.Log(CompileMessageWithTags(tags, message));
                }
            }
            else
            {
                OnLog?.Invoke(message, tags, stackTrace, logLevel);
            }
        }
        
        [DebuggerHidden]
        public static string CompileMessageWithTags(string[] tags, string message)
        {
            if (tags.Length == 0)
            {
                return message;
            }
            return $"[{string.Join("][", tags)}] {message}";
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Exception(string tag1, Exception exception)
        {
            Log(tag1, exception.ToString(), LogLevel.Exception, exception.StackTrace);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Exception(string tag1, string tag2, Exception exception)
        {
            Log(tag1, tag2, exception.ToString(), LogLevel.Exception, exception.StackTrace);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Exception(string tag1, string message)
        {
            Log(tag1, message, LogLevel.Exception);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Exception(string tag1, string tag2, string message)
        {
            Log(tag1, tag2, message, LogLevel.Exception);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Exception(string tag1, string tag2, string tag3, string message)
        {
            Log(tag1, tag2, tag3, message, LogLevel.Exception);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Error(string tag1, Exception exception)
        {
            Log(tag1, exception.ToString(), LogLevel.Error, exception.StackTrace);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Error(string tag1, string message)
        {
            Log(tag1, message, LogLevel.Error);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Error(string tag1, string tag2, string message)
        {
            Log(tag1, tag2, message, LogLevel.Error);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Error(string tag1, string tag2, string tag3, string message)
        {
            Log(tag1, tag2, tag3, message, LogLevel.Error);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Warning(string tag1, string message)
        {
            Log(tag1, message, LogLevel.Warning);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Warning(string tag1, string tag2, string message)
        {
            Log(tag1, tag2, message, LogLevel.Warning);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Warning(string tag1, string tag2, string tag3, string message)
        {
            Log(tag1, tag2, tag3, message, LogLevel.Warning);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Info(string tag1, string message)
        {
            Log(tag1, message, LogLevel.Info);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Info(string tag1, string tag2, string message)
        {
            Log(tag1, tag2, message, LogLevel.Info);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Info(string tag1, string tag2, string tag3, string message)
        {
            Log(tag1, tag2, tag3, message, LogLevel.Info);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Debug(string tag1, string message)
        {
            Log(tag1, message, LogLevel.Debug);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Debug(string tag1, string tag2, string message)
        {
            Log(tag1, tag2, message, LogLevel.Debug);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Debug(string tag1, string tag2, string tag3, string message)
        {
            Log(tag1, tag2, tag3, message, LogLevel.Debug);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Verbose(string tag1, string message)
        {
            Log(tag1, message, LogLevel.Verbose);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Verbose(string tag1, string tag2, string message)
        {
            Log(tag1, tag2, message, LogLevel.Verbose);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Verbose(string tag1, string tag2, string tag3, string message)
        {
            Log(tag1, tag2, tag3, message, LogLevel.Verbose);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Verbose(string tag1, string tag2, string tag3, string tag4, string message)
        {
            Log(tag1, tag2, tag3, tag4, message, LogLevel.Verbose);
        }
        
        [DebuggerHidden]
        [UsedImplicitly]
        public static void Verbose(string message, string[] tags, string stackTrace = null)
        {
            Log(message, tags, LogLevel.Verbose, stackTrace);
        }
        #endregion
        
        #region Interactive mode
        public static DisposableAction RegisterCommand(string command, Action<string> action)
        {
            return Commander.RegisterCommand(command, action);
        }
        
        public static DisposableAction RegisterCommand(string command, Commander commander)
        {
            return Commander.RegisterCommand(command, commander);
        }
        
        public static void ExecuteCommand(string command, string args)
        {
            Commander.ExecuteCommand(command, args);
        }
        #endregion"
    }
}