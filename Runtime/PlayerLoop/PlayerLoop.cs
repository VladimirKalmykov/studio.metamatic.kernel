using Cysharp.Threading.Tasks;
using UnityEngine;

namespace PlayerLoop
{
    public class PlayerLoop: MonoBehaviour
    {
        public static InjectPlayerLoopTimings InjectPlayerLoopTimings = InjectPlayerLoopTimings.All;
        private static PlayerLoop _Instance;
        void Start()
        {
            UnityEngine.Debug.Log("<color=blue>UniTaskPlayerLoop</color> ready: " + PlayerLoopHelper.IsInjectedUniTaskPlayerLoop());
            PlayerLoopHelper.DumpCurrentPlayerLoop();
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
        private static void Initialize()
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
            var loop =  UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
            PlayerLoopHelper.Initialize(ref loop, InjectPlayerLoopTimings);
            Debug.Log($"Custom <color=blue>UniTask</color> player loop initialized and set to <color=blue>{InjectPlayerLoopTimings}</color>");
            #else
            Debug.Log("You can define CUSTOM_PLAYERLOOP_INITIALIZATION in Player Settings to configure custom player loop");
            #endif
        }
    }
}