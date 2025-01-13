using System;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Metamatic
{
    public class PlayerLoop: MonoBehaviour
    {
        public static InjectPlayerLoopTimings InjectPlayerLoopTimings = InjectPlayerLoopTimings.Minimum;
        private static PlayerLoop _Instance;
        void Start()
        {
            UnityEngine.Debug.Log("<color=blue>UniTaskPlayerLoop</color> ready: " + PlayerLoopHelper.IsInjectedUniTaskPlayerLoop());
            PlayerLoopHelper.DumpCurrentPlayerLoop();
        }

        // ReSharper disable once InconsistentNaming
        public static PlayerLoop Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new GameObject("PLAYER_LOOP").AddComponent<PlayerLoop>();
                    DontDestroyOnLoad(_Instance.gameObject);
                }

                return _Instance;
            }
        }
        
        [RuntimeInitializeOnLoadMethod]
        private static void Instantiate()
        {
            // TODO: Consider do not instantiate this
            if (_Instance == null)
            {
                _Instance = new GameObject("PLAYER_LOOP").AddComponent<PlayerLoop>();
                DontDestroyOnLoad(_Instance.gameObject);
            }
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void InitializeCustomPlayerLoop()
        {
            #if CUSTOM_PLAYERLOOP_INITIALIZATION
            /*
            * To make possible to configure custom player loop, define CUSTOM_PLAYERLOOP_INITIALIZATION in Player Settings
             * Also you need to edit UniTask method Init in UniTask/Runtime/PlayerLoopHelper.cs to skip the default initialization
             * if CUSTOM_PLAYERLOOP_INITIALIZATION is defined
             *
             * This is an example of this modification:
             *
             * #if !CUSTOM_PLAYERLOOP_INITIALIZATION
             * var playerLoop =
             * #if UNITY_2019_3_OR_NEWER
             * PlayerLoop.GetCurrentPlayerLoop();
             * #else
             * PlayerLoop.GetDefaultPlayerLoop();
             * #endif
             * Initialize(ref playerLoop);
             * #endif
             *
             * Unfortunately, UniTask 2.2.5 has a bug if you can call Initialize(ref playerLoop) twice,
             * so you have to comment out the default initialization if you want to use custom initialization
             *
             * TODO: investigate modern versions of UniTask to see if this bug is fixed
            */
            var playerLoop =
#if UNITY_2019_3_OR_NEWER
                UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
#else
                UnityEngine.LowLevel.PlayerLoop.GetDefaultPlayerLoop();
#endif
            PlayerLoopHelper.Initialize(ref playerLoop, InjectPlayerLoopTimings);
            Debug.Log($"Custom <color=blue>UniTask</color> player loop initialized and set to <color=blue>{InjectPlayerLoopTimings}</color>");
            #else
            Debug.Log("You can define CUSTOM_PLAYERLOOP_INITIALIZATION in Player Settings to configure custom player loop");
            #endif
        }
        
        [Flags]
        public enum MicroSteps: byte
        {
            Zero = 0,
            Alfa = 1 << 1,
            Beta = 1 << 2,
            Gamma = 1 << 3,
            Delta = 1 << 4,
            Epsilon = 1 << 5,
            Zeta = 1 << 6,
            Eta = 1 << 7
        }
        
        private const int ActionCapacityStep = 50;

        private MicroSteps m_CurrentMicroSteps = MicroSteps.Zero;
        private MicroSteps m_CurrentMicroFixedSteps = MicroSteps.Zero;
        private Action<MicroSteps>[] m_UpdateActions = new Action<MicroSteps>[50];
        private Action<MicroSteps>[] m_FixedUpdateActions = new Action<MicroSteps>[50];
        private Action[] m_DrawGizmosActions = new Action[50];
        private int m_UpdateActionsCount = 0;
        private int m_FixedUpdateActionsCount = 0;
        private int m_DrawGizmosActionsCount = 0;
        
        public MicroSteps currentMicroStep => m_CurrentMicroSteps;
        public MicroSteps currentMicroFixedStep => m_CurrentMicroFixedSteps;
        
        private void Update()
        {
            /* Set next micro step */
            unchecked
            {
                var next = (MicroSteps)((byte)m_CurrentMicroSteps << 1);
                m_CurrentMicroSteps = next == MicroSteps.Zero ? MicroSteps.Alfa : next;
                for (var i = 0; i < m_UpdateActionsCount; i++)
                {
                    m_UpdateActions[i].Invoke(m_CurrentMicroSteps);
                }
            }
        }

        private void FixedUpdate()
        {
            /* Set next micro fixed step */
            unchecked
            {
                var next = (MicroSteps)((byte)m_CurrentMicroFixedSteps << 1);
                m_CurrentMicroFixedSteps = next == MicroSteps.Zero ? MicroSteps.Alfa : next;
                for (var i = 0; i < m_FixedUpdateActionsCount; i++)
                {
                    m_FixedUpdateActions[i].Invoke(m_CurrentMicroFixedSteps);
                }
            }
        }
        
        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            for (var i = 0; i < m_DrawGizmosActionsCount; i++)
            {
                m_DrawGizmosActions[i].Invoke();
            }
        }
        #endif

        public static DisposableAction DrawGizmos(Action handler)
        {
            var instance = Instance;
            if (!instance.enabled)
            {
                instance.enabled = true;
            }
            instance.m_DrawGizmosActions[instance.m_DrawGizmosActionsCount] = handler;
            
            /* Detect that current action capacity is not enough */
            if (++instance.m_DrawGizmosActionsCount >= instance.m_DrawGizmosActions.Length)
            {
                Array.Resize(ref instance.m_DrawGizmosActions, instance.m_DrawGizmosActions.Length + ActionCapacityStep);
            }
            
            return new DisposableAction(() =>
            {
                for (int i = 0; i < instance.m_DrawGizmosActionsCount; i++)
                {
                    if (instance.m_DrawGizmosActions[i] == handler)
                    {
                        instance.m_DrawGizmosActions[i] = null;
                        
                        /* Shift rest of the actions */
                        for (int j = i; j < instance.m_DrawGizmosActionsCount - 1; j++)
                        {
                            instance.m_DrawGizmosActions[j] = instance.m_DrawGizmosActions[j + 1];
                        }
                        instance.m_DrawGizmosActionsCount--;
                        
                        if (instance.m_UpdateActionsCount == 0 && instance.m_FixedUpdateActionsCount == 0)
                        {
                            instance.enabled = false;
                        }
                        break;
                    }
                }
            });
        }

        public static DisposableAction MicroStep(MicroSteps steps, Action action)
        {
            var instance = Instance;
            if (!instance.enabled)
            {
                instance.enabled = true;
            }
            void Handler(MicroSteps currentSteps)
            {
                if ((steps & currentSteps) == currentSteps)
                {
                    action();
                }
            }
            instance.m_UpdateActions[_Instance.m_UpdateActionsCount] = Handler;
            
            /* Detect that current action capacity is not enough */
            if (++instance.m_UpdateActionsCount >= instance.m_UpdateActions.Length)
            {
                Array.Resize(ref instance.m_UpdateActions, instance.m_UpdateActions.Length + ActionCapacityStep);
            }
            
            return new DisposableAction(() =>
            {
                for (int i = 0; i < instance.m_UpdateActionsCount; i++)
                {
                    if (instance.m_UpdateActions[i] == Handler)
                    {
                        instance.m_UpdateActions[i] = null;
                        
                        /* Shift rest of the actions */
                        for (int j = i; j < instance.m_UpdateActionsCount - 1; j++)
                        {
                            instance.m_UpdateActions[j] = instance.m_UpdateActions[j + 1];
                        }
                        instance.m_UpdateActionsCount--;
                        
                        if (instance.m_UpdateActionsCount == 0 && instance.m_FixedUpdateActionsCount == 0)
                        {
                            instance.enabled = false;
                        }
                        break;
                    }
                }
            });
        }
        
        public static DisposableAction FixedMicroStep(MicroSteps steps, Action action)
        {
            
            var instance = Instance;
            if (!instance.enabled)
            {
                instance.enabled = true;
            }
            void Handler(MicroSteps currentSteps)
            {
                if ((steps & currentSteps) == currentSteps)
                {
                    action();
                }
            }
            instance.m_FixedUpdateActions[_Instance.m_FixedUpdateActionsCount] = Handler;
            
            /* Detect that current action capacity is not enough */
            if (++instance.m_FixedUpdateActionsCount >= instance.m_FixedUpdateActions.Length)
            {
                Array.Resize(ref instance.m_FixedUpdateActions, instance.m_FixedUpdateActions.Length + ActionCapacityStep);
            }
            
            return new DisposableAction(() =>
            {
                if (instance.Equals(null)) return;
                for (int i = 0; i < instance.m_FixedUpdateActionsCount; i++)
                {
                    if (instance.m_FixedUpdateActions[i] == Handler)
                    {
                        instance.m_FixedUpdateActions[i] = null;
                        
                        /* Shift rest of the actions */
                        for (int j = i; j < instance.m_FixedUpdateActionsCount - 1; j++)
                        {
                            instance.m_FixedUpdateActions[j] = instance.m_FixedUpdateActions[j + 1];
                        }
                        instance.m_FixedUpdateActionsCount--;
                        
                        if (instance.m_UpdateActionsCount == 0 && instance.m_FixedUpdateActionsCount == 0)
                        {
                            instance.enabled = false;
                        }
                        
                        break;
                    }
                }
            });
        }

        public static async UniTask EachFrame(Action<float> action, float duration, PlayerLoopTiming timing = PlayerLoopTiming.Update,
            Signal signal = default)
        {
            if (duration <= 0f)
            {
                return;
            }
            var startTime = Time.time;
            await UniTask.WaitUntil(() =>
            {
                var progress = Mathf.Clamp01((Time.time - startTime) / duration);
                action(progress);
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                return progress == 1f || signal.IsCancellationRequested;
            }, timing);
            signal.ThrowIfRaised();
        }
        
        public static DisposableAction EachFrame(Action action, PlayerLoopTiming timing = PlayerLoopTiming.Update)
        {
            var isActive = true;
            UniTask.WaitUntil(() =>
            {
                action();
                return !isActive;
            }, timing);
            
            return new DisposableAction(() => isActive = false);
        }
        
        /// <summary>
        /// Subscribe to the loop with delay. Action will be called every `seconds` seconds.
        /// </summary>
        /// <param name="seconds"></param>
        /// <param name="action"></param>
        /// <param name="timing"></param>
        /// <returns></returns>
        public static DisposableAction LoopDelay(float seconds, Action action, PlayerLoopTiming timing = PlayerLoopTiming.Update)
        {
            var currentTime = Time.time;
            var isActive = true;
            UniTask.WaitUntil(() =>
            {
                if (Time.time - currentTime >= seconds)
                {
                    currentTime = Time.time;
                    action();
                    return !isActive;
                }
                return false;
            }, timing);
            
            return new DisposableAction(() => isActive = false);
        }
        
        /// <summary>
        /// Subscribe to the loop with delay. Action will be called every `seconds` seconds provided by the `getSeconds` function.
        /// Each time delay is reached, the `getSeconds` function will be called to get the next delay.
        /// </summary>
        /// <param name="seconds"></param>
        /// <param name="action"></param>
        /// <param name="timing"></param>
        /// <returns></returns>
        public static DisposableAction LoopDelay(Func<float> getSeconds, Action action, PlayerLoopTiming timing = PlayerLoopTiming.Update)
        {
            var currentTime = Time.time;
            var isActive = true;
            var nextSeconds = getSeconds();
            UniTask.WaitUntil(() =>
            {
                if (Time.time - currentTime >= nextSeconds)
                {
                    currentTime = Time.time;
                    nextSeconds = getSeconds();
                    action();
                    return !isActive;
                }
                return false;
            }, timing);
            
            return new DisposableAction(() => isActive = false);
        }
        
        /// <summary>
        /// Similar to UniTask.WaitUntil but with signal support.
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="timing"></param>
        /// <param name="signal"></param>
        public static async UniTask EachFrameUntil(Func<bool> predicate, PlayerLoopTiming timing = PlayerLoopTiming.Update,
            Signal signal = default)
        {
            await UniTask.WaitUntil(() => signal.IsCancellationRequested || predicate(), timing);
            signal.ThrowIfRaised();
        }

        private static Stopwatch m_Stopwatch;
        private static int m_StopwatchInquiries;
        /// <summary>
        /// Similar to UniTask.Delay but with signal support.
        /// </summary>
        /// <param name="seconds">Delay duration in seconds.</param>
        /// <param name="timing">Player loop timing.</param>
        /// <param name="signal">Cancellation signal.</param>
        public static async UniTask Delay(float seconds, PlayerLoopTiming timing = PlayerLoopTiming.Update, Signal signal = default)
        {
            #if UNITY_EDITOR
            bool isRealTime = PlayerLoopHelper.IsMainThread && !UnityEditor.EditorApplication.isPlaying;
            #else
            bool isRealTime = false;
            #endif

            if (isRealTime)
            {
                m_StopwatchInquiries++;
                if (m_StopwatchInquiries == 1)
                {
                    m_Stopwatch = Stopwatch.StartNew();
                }
            }

            // Determine start time based on whether real time is used.
            float startTime = isRealTime ? (float)m_Stopwatch.Elapsed.TotalSeconds : Time.time;

            await UniTask.WaitWhile(() =>
            {
                float currentTime = isRealTime ? (float)m_Stopwatch.Elapsed.TotalSeconds : Time.time;
                return !signal.IsCancellationRequested && currentTime - startTime < seconds;
            }, timing);
            
            if (isRealTime)
            {
                m_StopwatchInquiries--;
                if (m_StopwatchInquiries == 0)
                {
                    m_Stopwatch.Stop();
                    m_Stopwatch = null;
                }
            }

            signal.ThrowIfRaised();
        }
        
        public static async UniTask Delay(TimeSpan timeSpan, PlayerLoopTiming timing = PlayerLoopTiming.Update, Signal signal = default)
        {
            var seconds = (float)timeSpan.TotalSeconds;
#if UNITY_EDITOR
            bool isRealTime = PlayerLoopHelper.IsMainThread && !UnityEditor.EditorApplication.isPlaying;
#else
            bool isRealTime = false;
#endif

            if (isRealTime)
            {
                m_StopwatchInquiries++;
                if (m_StopwatchInquiries == 1)
                {
                    m_Stopwatch = Stopwatch.StartNew();
                }
            }

            // Determine start time based on whether real time is used.
            float startTime = isRealTime ? (float)m_Stopwatch.Elapsed.TotalSeconds : Time.time;

            await UniTask.WaitWhile(() =>
            {
                float currentTime = isRealTime ? (float)m_Stopwatch.Elapsed.TotalSeconds : Time.time;
                return !signal.IsCancellationRequested && currentTime - startTime < seconds;
            }, timing);
            
            if (isRealTime)
            {
                m_StopwatchInquiries--;
                if (m_StopwatchInquiries == 0)
                {
                    m_Stopwatch.Stop();
                    m_Stopwatch = null;
                }
            }

            signal.ThrowIfRaised();
        }
        
        public static Action<T> CreateNextFrameAction<T>(Action<T> action, Signal cancellationSignal = default)
        {
            if (!Application.isPlaying)
            {
                return action;
            }
            return (T value) =>
            {
                NextFrame(value).Forget();
            };

            async UniTaskVoid NextFrame(T prop)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
                if (cancellationSignal.isRaised) return;
                action(prop);
            }
        }
        
        public static async UniTask WaitNextFrame(Signal signal = default, PlayerLoopTiming timing = PlayerLoopTiming.Update)
        {
            await UniTask.NextFrame(timing);
            signal.ThrowIfRaised();
        }
    }
}