using Cysharp.Threading.Tasks;
using UnityEngine;

namespace PlayerLoop
{
    public class PlayerLoop: MonoBehaviour
    {
        private static PlayerLoop _Instance;
        void Start()
        {
            UnityEngine.Debug.Log("<color=blue>UniTaskPlayerLoop</color> ready: " + PlayerLoopHelper.IsInjectedUniTaskPlayerLoop());
            PlayerLoopHelper.DumpCurrentPlayerLoop();
            
            // Optimize custom player loop of UniTask for performance by removing unnecessary timings.
            var loop =  UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
            PlayerLoopHelper.Initialize(ref loop, InjectPlayerLoopTimings.Minimum); // minimum is Update | FixedUpdate | LastPostLateUpdate
        }

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            if (_Instance == null)
            {
                _Instance = new GameObject("PLAYER_LOOP").AddComponent<PlayerLoop>();
                DontDestroyOnLoad(_Instance.gameObject);
            }
        }
    }
}