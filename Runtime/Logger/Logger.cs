using Metamatic.Actions;
using Metamatic.Exceptions;
using UnityEngine;

namespace Metamatic
{
    public class Logger
    {
        private static bool _Initialized;
        /* Run this method in the beginning of the game */
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static async void Initialization()
        {
            if (_Initialized)
            {
                throw new System.Exception("Logger already initialized");
            }
            _Initialized = true;
            Dispatcher.global.Subscribe<FatalError>(e =>
            {
                LogError(e.Message);
            });
        }
        public static void Log(string message)
        {
            #if DEBUG
            Debug.Log("<b>Logger</b>: " + message);
            #endif
            #if METAMATIC_TERMINAL
            Terminal.Log(message);
            #endif
        }
        
        public static void LogError(string message)
        {
            #if METAMATIC_TERMINAL
            Debug.LogError("<b>Logger</b>: " + message);
            Terminal.LogError(message);
            #endif
        }
        
        public static void LogError(System.Exception e)
        {
            #if METAMATIC_TERMINAL
            Debug.LogError("<b>Logger</b>: " + e.Message);
            Terminal.LogError(e.Message);
            #endif
        }
    }
}